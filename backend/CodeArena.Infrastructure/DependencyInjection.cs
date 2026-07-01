using CodeArena.Application.Interfaces;
using CodeArena.Infrastructure.Persistence;
using CodeArena.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CodeArena.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // PostgreSQL / EF Core
        services.AddDbContext<CodeArenaDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(CodeArenaDbContext).Assembly.FullName)
            )
        );
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<CodeArenaDbContext>());

        // Redis — IConnectionMultiplexer (shared singleton) + IDistributedCache
        var redisConn = configuration["REDIS_CONNECTION"] ?? "redis:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
        services.AddStackExchangeRedisCache(options => options.Configuration = redisConn);

        // Domain services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IEmailService, EmailService>();

        // Notification pusher — publishes to Redis pub/sub, relayed to SignalR by RedisNotificationRelay (API)
        services.AddScoped<INotificationPusher, RedisPublishPusher>();

        // CompetitionStatusUpdater BackgroundService replaced by Hangfire recurring job (registered in Program.cs/Worker)

        return services;
    }
}
