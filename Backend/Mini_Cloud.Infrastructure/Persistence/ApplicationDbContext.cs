using Microsoft.EntityFrameworkCore;
using Mini_Cloud.Application.Common.Interfaces;
using Mini_Cloud.Domain.Entities;

namespace Mini_Cloud.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.Property(u => u.UserName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.Gmail)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.HasIndex(u => u.Gmail)
                    .IsUnique();

                entity.Property(u => u.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.CreatedAt)
                    .IsRequired();

                entity.HasMany(u => u.Files)
                    .WithOne(f => f.User)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StoredFile>(entity =>
            {
                entity.HasKey(f => f.Id);

                entity.Property(f => f.FileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(f => f.FilePath)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(f => f.FileSize)
                    .IsRequired();

                entity.Property(f => f.UserId)
                    .IsRequired();

                entity.HasIndex(f => f.UserId);

                entity.Property(f => f.CreatedAt)
                    .IsRequired();

                entity.Property(f => f.IsShared)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(f => f.ShareToken)
                    .HasMaxLength(64);

                entity.HasIndex(f => f.ShareToken)
                    .IsUnique();

                entity.Property(f => f.ShareExpiresAt);
            });
        }
    }
}
