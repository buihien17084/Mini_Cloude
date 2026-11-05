using MediatR;
using Microsoft.Extensions.Logging;
using Mini_Cloud.Application.Common.Exceptions;
using Mini_Cloud.Application.Common.Interfaces;
using Mini_Cloud.Application.Common.Models;
using Microsoft.EntityFrameworkCore;


    public record DeleteFileCommand(
    Guid UserId,
    Guid FileId
) : IRequest<FileDetailDto>;

    public class DeleteFileCommandHandler : IRequestHandler<DeleteFileCommand, FileDetailDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IFileStorageService _storageService;
        private readonly ILogger<DeleteFileCommandHandler> _logger;

        public DeleteFileCommandHandler(
            IApplicationDbContext context,
            IFileStorageService storageService,
            ILogger<DeleteFileCommandHandler> logger)
        {
            _context = context;
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<FileDetailDto> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId == Guid.Empty)
            {
                throw new UnauthorizedException();
            }

            var file = await _context.StoredFiles
                .FirstOrDefaultAsync(f => f.Id == request.FileId && f.UserId == request.UserId, cancellationToken);

            if (file == null)
            {
                throw new NotFoundException("Không tìm thấy file hoặc bạn không có quyền truy cập.");
            }

            var resultDto = new FileDetailDto(
                file.Id,
                file.FileName,
                file.FileSize,
                file.CreatedAt,
                file.IsShared,
                file.ShareToken,
                file.ShareExpiresAt
            );

            var physicalPath = file.FilePath;

            _context.StoredFiles.Remove(file);
            await _context.SaveChangesAsync(cancellationToken);
            try
            {
                _storageService.DeleteFile(physicalPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Đã xóa bản ghi file khỏi database nhưng xóa file vật lý thất bại. Tên file trên đĩa: {FilePath}", physicalPath);
            }

            return resultDto;
        }
    }

