using MediatR;
using Mini_Cloud.Application.Common.Exceptions;
using Mini_Cloud.Application.Common.Interfaces;
using Mini_Cloud.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Mini_Cloud.Application.Features.Files;

public record DownloadSharedFileQuery(
 string Token
) : IRequest<DownloadFileResult>;

public class DownloadSharedFileQueryHandler : IRequestHandler<DownloadSharedFileQuery, DownloadFileResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _storageService;

    public DownloadSharedFileQueryHandler(
        IApplicationDbContext context,
        IFileStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }

    public async Task<DownloadFileResult> Handle(DownloadSharedFileQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            throw new NotFoundException("Link chia sẻ không tồn tại hoặc đã hết hạn.");
        }

        var now = DateTime.UtcNow;

        var file = await _context.StoredFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f =>
                f.ShareToken == request.Token &&
                f.IsShared &&
                (f.ShareExpiresAt == null || f.ShareExpiresAt > now),
                cancellationToken);
        if (file == null)
        {
            throw new NotFoundException("Link chia sẻ không tồn tại hoặc đã hết hạn.");
        }

        if (!_storageService.FileExists(file.FilePath))
        {
            throw new NotFoundException("Không tìm thấy file trên hệ thống lưu trữ.");
        }

        var stream = _storageService.OpenReadStream(file.FilePath);
        var contentType = DownloadFileQueryHandler.GetContentType(file.FileName);

        return new DownloadFileResult(stream, file.FileName, contentType);

    }
}

