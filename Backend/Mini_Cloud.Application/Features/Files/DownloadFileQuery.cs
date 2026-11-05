using MediatR;
using Mini_Cloud.Application.Common.Exceptions;
using Mini_Cloud.Application.Common.Interfaces;
using Mini_Cloud.Application.Common.Models;
using Microsoft.EntityFrameworkCore;


namespace Mini_Cloud.Application.Features.Files
{
    public record DownloadFileQuery(
     Guid UserId,
     Guid FileId
 ) : IRequest<DownloadFileResult>;

    public class DownloadFileQueryHandler : IRequestHandler<DownloadFileQuery, DownloadFileResult>
    {
        private readonly IApplicationDbContext _context;
        private readonly IFileStorageService _storageService;

        public DownloadFileQueryHandler(
            IApplicationDbContext context,
            IFileStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        public async Task<DownloadFileResult> Handle(DownloadFileQuery request, CancellationToken cancellationToken)
        {
            if (request.UserId == Guid.Empty)
            {
                throw new UnauthorizedException();
            }
            var file = await _context.StoredFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == request.FileId && f.UserId == request.UserId, cancellationToken);

            if (file == null)
            {
                throw new NotFoundException("Không tìm thấy file hoặc bạn không có quyền truy cập.");
            }

            if (!_storageService.FileExists(file.FilePath))
            {
                throw new NotFoundException("Không tìm thấy file vật lý trên hệ thống lưu trữ.");
            }

            var stream = _storageService.OpenReadStream(file.FilePath);
            var contentType = GetContentType(file.FileName);

            return new DownloadFileResult(stream, file.FileName, contentType);
        }

        internal static string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".txt" => "text/plain",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".mp3" => "audio/mpeg",
                ".mp4" => "video/mp4",
                _ => "application/octet-stream"
            };
        }
    }
}
