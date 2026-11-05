using System;
using System.Collections.Generic;
using System.Text;

namespace Mini_Cloud.Domain.Entities
{
    public class StoredFile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsShared { get; set; } = false;
        public string? ShareToken { get; set; }
        public DateTime? ShareExpiresAt { get; set; }

        public User? User { get; set; }
    }
}
