using CodeArena.Application.Interfaces;
using CodeArena.Domain.Enums;
using CodeArena.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CodeArena.Infrastructure.BackgroundServices;

public class CompetitionStatusUpdater(
    IServiceScopeFactory scopeFactory,
    ILogger<CompetitionStatusUpdater> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("CompetitionStatusUpdater started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateStatusesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error in CompetitionStatusUpdater");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task UpdateStatusesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CodeArenaDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var now = DateTime.UtcNow;

        var competitions = await db.Competitions
            .Where(c => c.Status == CompetitionStatus.Upcoming || c.Status == CompetitionStatus.Ongoing)
            .ToListAsync(ct);

        int updated = 0;
        foreach (var comp in competitions)
        {
            var endDate = comp.StartDate.Add(comp.Duration);

            if (comp.Status == CompetitionStatus.Upcoming)
            {
                // Reminder: notifier 1h avant le démarrage (une seule fois)
                var minutesUntilStart = (comp.StartDate - now).TotalMinutes;
                if (minutesUntilStart <= 60 && minutesUntilStart > 0 && comp.StartReminderSentAt is null)
                {
                    comp.StartReminderSentAt = now;
                    updated++;
                    logger.LogInformation("Competition {Id} ({Name}) → sending 1h reminder", comp.Id, comp.Name);
                    _ = NotifyAllUsersAsync(db, notificationService,
                        NotificationType.CompetitionStarting,
                        $"Compétition dans 1h — {comp.Name}",
                        $"La compétition \"{comp.Name}\" commence dans moins d'une heure. Préparez-vous !",
                        ct);
                }

                if (now >= comp.StartDate)
                {
                    comp.Status = CompetitionStatus.Ongoing;
                    updated++;
                    logger.LogInformation("Competition {Id} ({Name}) → Ongoing", comp.Id, comp.Name);
                    _ = NotifyAllUsersAsync(db, notificationService,
                        NotificationType.CompetitionStarted,
                        $"Compétition démarrée — {comp.Name}",
                        $"La compétition \"{comp.Name}\" vient de commencer. Bonne chance !",
                        ct);
                }
            }
            else if (comp.Status == CompetitionStatus.Ongoing && now >= endDate)
            {
                comp.Status = CompetitionStatus.Finished;
                updated++;
                logger.LogInformation("Competition {Id} ({Name}) → Finished", comp.Id, comp.Name);
            }
        }

        if (updated > 0)
            await db.SaveChangesAsync(ct);
    }

    private async Task NotifyAllUsersAsync(
        CodeArenaDbContext db,
        INotificationService notificationService,
        NotificationType type,
        string title,
        string body,
        CancellationToken ct)
    {
        try
        {
            var userIds = await db.Users
                .Where(u => u.IsActive)
                .Select(u => u.Id)
                .ToListAsync(ct);

            foreach (var userId in userIds)
            {
                await notificationService.CreateAsync(userId, type, title, body, ct);
            }

            logger.LogInformation("Sent {Type} notification to {Count} users", type, userIds.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send competition notification {Type}", type);
        }
    }
}
