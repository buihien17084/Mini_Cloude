using System;
using System.Collections.Generic;
using System.Text;

namespace Mini_Cloud.Application.Common.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(Stream stream, string originalFileName, CancellationToken cancellationToken = default);
        Stream OpenReadStream(string fileNameOnDisk);
        bool FileExists(string fileNameOnDisk);
        void DeleteFile(string fileNameOnDisk);
        string GetSafeFullPath(string fileNameOnDisk);
    }
}
