using MediatR;
using Microsoft.Extensions.Logging;
using Mini_Cloud.Application.Common.Exceptions;
using Mini_Cloud.Application.Common.Interfaces;
using Mini_Cloud.Application.Common.Models;
using Mini_Cloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;


namespace Mini_Cloud.Application.Features.Files;

public record UploadFileCommand(
    Guid UserId,
    Stream FileStream,
    string FileName,
    long FileSize
) : IRequest<FileDetailDto>;
public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, FileDetailDto>
    {
        private const long MaxSingleFileSize = 100L * 1024 * 1024; // 100 MB
        private const long MaxUserTotalQuota = 500L * 1024 * 1024; // 500 MB

        private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> UserLocks = new();

        private readonly IApplicationDbContext _context;
        private readonly IFileStorageService _storageService;
        private readonly ILogger<UploadFileCommandHandler> _logger;

        public UploadFileCommandHandler(
            IApplicationDbContext context,
            IFileStorageService storageService,
            ILogger<UploadFileCommandHandler> logger)
        {
            _context = context;
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<FileDetailDto> Handle(UploadFileCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId == Guid.Empty)
            {
                throw new UnauthorizedException();
            }

            if (request.FileSize <= 0)
            {
                throw new BadRequestException("File không được để rỗng.");
            }

            if (request.FileSize > MaxSingleFileSize)
            {
                throw new PayloadTooLargeException("Kích thước file vượt quá giới hạn tối đa 100MB.");
            }

            var sanitizedFileName = Path.GetFileName(request.FileName?.Trim());
            if (string.IsNullOrWhiteSpace(sanitizedFileName) || sanitizedFileName.Length > 255 || sanitizedFileName.Any(char.IsControl))
            {
                throw new BadRequestException("Tên file không hợp lệ hoặc vượt quá 255 ký tự.");
            }

            // Synchronize uploads per user to prevent concurrent race condition on quota
            var userLock = UserLocks.GetOrAdd(request.UserId, _ => new SemaphoreSlim(1, 1));
            await userLock.WaitAsync(cancellationToken);

            string? physicalFileName = null;
            try
            {
                var currentUsed = await _context.StoredFiles
                    .Where(f => f.UserId == request.UserId)
                    .SumAsync(f => (long?)f.FileSize, cancellationToken) ?? 0L;

                if (currentUsed + request.FileSize > MaxUserTotalQuota)
                {
                    throw new PayloadTooLargeException("Tổng dung lượng lưu trữ vượt quá giới hạn 500MB.");
                }

                // Save physical file
                physicalFileName = await _storageService.SaveFileAsync(
                    request.FileStream,
                    sanitizedFileName,
                    cancellationToken);

                var storedFile = new StoredFile
                {
                    Id = Guid.NewGuid(),
                    FileName = sanitizedFileName,
                    FilePath = physicalFileName,
                    FileSize = request.FileSize,
                    UserId = request.UserId,
                    CreatedAt = DateTime.UtcNow,
                    IsShared = false,
                    ShareToken = null,
                    ShareExpiresAt = null
                };

                _context.StoredFiles.Add(storedFile);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi lưu metadata file vào cơ sở dữ liệu. Tiến hành dọn dẹp file vật lý: {PhysicalFile}", physicalFileName);
                    try
                    {
                        _storageService.DeleteFile(physicalFileName);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError(cleanupEx, "Lỗi khi dọn dẹp file vật lý mồ côi: {PhysicalFile}", physicalFileName);
                    }
                    throw;
                }

                return new FileDetailDto(
                    storedFile.Id,
                    storedFile.FileName,
                    storedFile.FileSize,
                    storedFile.CreatedAt,
                    storedFile.IsShared,
                    storedFile.ShareToken,
                    storedFile.ShareExpiresAt
                );
            }
            finally
            {
                userLock.Release();
            }
        }
    }


