using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mini_Cloud.Application.Common.Interfaces;


namespace Mini_Cloud.Infrastructure.Storage
{
    internal class LocalFileStorageService : IFileStorageService
    {
        private const int MaxExtensionLength = 20;

        private readonly string _rootPath;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
        {
            _logger = logger;

            var configured = configuration["Storage:RootPath"];
            if (string.IsNullOrWhiteSpace(configured))
            {
                configured = "Storage";
            }

            _rootPath = Path.GetFullPath(configured);
            Directory.CreateDirectory(_rootPath);
        }

        public async Task<string> SaveFileAsync(Stream stream, string originalFileName, CancellationToken cancellationToken = default)
        {
            var fileNameOnDisk = Guid.NewGuid().ToString("N") + GetSafeExtension(originalFileName);
            var fullPath = GetSafeFullPath(fileNameOnDisk);

            try
            {
                await using var target = new FileStream(
                    fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                    bufferSize: 81920, useAsync: true);
                await stream.CopyToAsync(target, cancellationToken);
            }
            catch
            {
                // Không để lại file dở dang nếu copy bị lỗi hoặc bị hủy
                TryDelete(fullPath);
                throw;
            }

            return fileNameOnDisk;
        }

        public Stream OpenReadStream(string fileNameOnDisk)
        {
            var fullPath = GetSafeFullPath(fileNameOnDisk);
            return new FileStream(
                fullPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 81920, useAsync: true);
        }
        public void DeleteFile(string fileNameOnDisk)
        {
            var fullPath = GetSafeFullPath(fileNameOnDisk);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        public string GetSafeFullPath(string fileNameOnDisk)
        {
            if (string.IsNullOrWhiteSpace(fileNameOnDisk) ||
                fileNameOnDisk != Path.GetFileName(fileNameOnDisk) ||
                fileNameOnDisk.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException("Tên file trên đĩa không hợp lệ.", nameof(fileNameOnDisk));
            }

            var fullPath = Path.GetFullPath(Path.Combine(_rootPath, fileNameOnDisk));
            var rootWithSeparator = _rootPath.EndsWith(Path.DirectorySeparatorChar)
                ? _rootPath
                : _rootPath + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(rootWithSeparator, StringComparison.Ordinal))
            {
                throw new ArgumentException("Đường dẫn file nằm ngoài thư mục lưu trữ.", nameof(fileNameOnDisk));
            }

            return fullPath;
        }
        private static string GetSafeExtension(string originalFileName)
        {
            var ext = Path.GetExtension(Path.GetFileName(originalFileName ?? string.Empty));
            if (string.IsNullOrEmpty(ext) || ext.Length > MaxExtensionLength)
            {
                return string.Empty;
            }

            // Chỉ giữ đuôi gồm chữ/số để tên trên đĩa luôn an toàn
            foreach (var c in ext.AsSpan(1))
            {
                if (!char.IsAsciiLetterOrDigit(c))
                {
                    return string.Empty;
                }
            }

            return ext.ToLowerInvariant();
        }
        private void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không dọn được file dở dang: {Path}", path);
            }
        }
    }
}
