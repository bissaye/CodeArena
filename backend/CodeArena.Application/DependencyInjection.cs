using CodeArena.Application.Interfaces;
using CodeArena.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CodeArena.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IBadgeService, BadgeService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ICompetitionService, CompetitionService>();
        services.AddScoped<ILeaderboardService, LeaderboardService>();
        services.AddSingleton<IMarkdownSanitizerService, MarkdownSanitizerService>();
        services.AddScoped<IProblemService, ProblemService>();
        services.AddScoped<ISubmissionService, SubmissionService>();
        services.AddScoped<IUserService, UserService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
