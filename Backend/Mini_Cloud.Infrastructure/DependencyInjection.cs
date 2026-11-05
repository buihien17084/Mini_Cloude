using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mini_Cloud.Application.Common.Interfaces;
using Mini_Cloud.Infrastructure.Persistence;
using Mini_Cloud.Infrastructure.Security;
using Mini_Cloud.Infrastructure.Storage;

namespace Mini_Cloud.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Thiếu ConnectionStrings:DefaultConnection. Hãy cấu hình bằng User Secrets hoặc biến môi trường; ứng dụng không tự chuyển sang InMemory Database.");
        }

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 46));
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseMySql(
                connectionString,
                serverVersion,
                mySqlOptions => mySqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null));
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}



