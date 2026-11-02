using System;
using System.Collections.Generic;
using System.Text;

namespace Mini_Cloud.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserName { get; set; } = string.Empty;
        public string Gmail { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<StoredFile> Files { get; set; } = new List<StoredFile>();
    }
}
