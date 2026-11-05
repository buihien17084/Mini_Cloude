
namespace Mini_Cloud.Application.Common.Models;

public record DownloadFileResult(
Stream Stream,
string FileName,
string ContentType
) : IDisposable
{
    public void Dispose()
    {
        Stream?.Dispose();
    }
}
    

