using System;
using System.Collections.Generic;
using System.Text;
using Mini_Cloud.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace Mini_Cloud.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<StoredFile> StoredFiles { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
