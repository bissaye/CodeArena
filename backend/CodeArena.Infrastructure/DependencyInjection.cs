using CodeArena.Application.Interfaces;
using CodeArena.Infrastructure.BackgroundServices;
using CodeArena.Infrastructure.Persistence;
using CodeArena.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeArena.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<CodeArenaDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(CodeArenaDbContext).Assembly.FullName)
            )
        );

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<CodeArenaDbContext>());
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IEmailService, EmailService>();

        services.AddHostedService<CompetitionStatusUpdater>();

        return services;
    }
}
