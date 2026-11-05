using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mini_Cloud.Application.Common.Interfaces;
using Mini_Cloud.Infrastructure.Persistence;
using Mini_Cloud.Infrastructure.Security;
using Mini_Cloud.Infrastructure.Storage;

namespace MiniCloud.Tests.Common;

    public class TestBase : IDisposable
{

    protected readonly ApplicationDbContext DbContext;
    protected readonly IPasswordHasher PasswordHasher;
    protected readonly IJwtTokenGenerator JwtTokenGenerator;
    protected readonly string TestStorageDir;
    protected readonly IFileStorageService StorageService;
    protected readonly ILoggerFactory LoggerFactory;

    public TestBase()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        DbContext = new ApplicationDbContext(dbOptions);
        PasswordHasher = new BCryptPasswordHasher();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:SecretKey", "SuperSecretKeyForTestingCloudDriveApplication2026!" },
            { "Jwt:Issuer", "CloudDriveTest" },
            { "Jwt:Audience", "CloudDriveTestClient" },
            { "Jwt:ExpiryInHours", "24" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        JwtTokenGenerator = new JwtTokenGenerator(config);

        TestStorageDir = Path.Combine(Path.GetTempPath(), "CloudDriveTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(TestStorageDir);

        var storageConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Storage:RootPath", TestStorageDir }
            })
            .Build();

        LoggerFactory = new NullLoggerFactory();
        StorageService = new LocalFileStorageService(storageConfig, NullLogger<LocalFileStorageService>.Instance);
    }

    public void Dispose()
    {
        DbContext.Dispose();
        try
        {
            if (Directory.Exists(TestStorageDir))
            {
                Directory.Delete(TestStorageDir, recursive: true);
            }
        }
        catch
        {
        }
    }
}

