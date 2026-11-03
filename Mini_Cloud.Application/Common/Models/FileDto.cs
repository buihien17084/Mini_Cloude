using System;
using System.Collections.Generic;
using System.Text;

namespace Mini_Cloud.Application.Common.Models;

    public record FileListDto(
    Guid Id,
    string FileName,
    long FileSize,
    DateTime CreatedAt,
    bool IsShared = false
);

    public record FileDetailDto(
        Guid Id,
        string FileName,
        long FileSize,
        DateTime CreatedAt,
        bool IsShared = false,
        string? ShareToken = null,
        DateTime? ShareExpiresAt = null
    );


