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
        // Each iteration gets its own isolated scope — no shared DbContext with notifications
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CodeArenaDbContext>();
        var now = DateTime.UtcNow;

        var competitions = await db.Competitions
            .Where(c => c.Status == CompetitionStatus.Upcoming || c.Status == CompetitionStatus.Ongoing)
            .ToListAsync(ct);

        // Collect notifications to send — fire AFTER SaveChanges, each in its own scope
        var pendingNotifications = new List<(NotificationType type, string title, string body)>();
        int updated = 0;

        foreach (var comp in competitions)
        {
            var endDate = comp.StartDate.Add(comp.Duration);

            if (comp.Status == CompetitionStatus.Upcoming)
            {
                var minutesUntilStart = (comp.StartDate - now).TotalMinutes;
                if (minutesUntilStart <= 60 && minutesUntilStart > 0 && comp.StartReminderSentAt is null)
                {
                    comp.StartReminderSentAt = now;
                    updated++;
                    logger.LogInformation("Competition {Id} ({Name}) → sending 1h reminder", comp.Id, comp.Name);
                    pendingNotifications.Add((
                        NotificationType.CompetitionStarting,
                        $"Compétition dans 1h — {comp.Name}",
                        $"La compétition \"{comp.Name}\" commence dans moins d'une heure. Préparez-vous !"));
                }

                if (now >= comp.StartDate)
                {
                    comp.Status = CompetitionStatus.Ongoing;
                    updated++;
                    logger.LogInformation("Competition {Id} ({Name}) → Ongoing", comp.Id, comp.Name);
                    pendingNotifications.Add((
                        NotificationType.CompetitionStarted,
                        $"Compétition démarrée — {comp.Name}",
                        $"La compétition \"{comp.Name}\" vient de commencer. Bonne chance !"));
                }
            }
            else if (comp.Status == CompetitionStatus.Ongoing && now >= endDate)
            {
                comp.Status = CompetitionStatus.Finished;
                updated++;
                logger.LogInformation("Competition {Id} ({Name}) → Finished", comp.Id, comp.Name);
            }
        }

        // Commit status changes first — no concurrent operations on this db instance
        if (updated > 0)
            await db.SaveChangesAsync(ct);

        // Fire-and-forget notifications — each creates its own scope and DbContext
        foreach (var (type, title, body) in pendingNotifications)
            _ = NotifyAllUsersAsync(type, title, body);
    }

    private async Task NotifyAllUsersAsync(NotificationType type, string title, string body)
    {
        try
        {
            // Own scope — completely isolated from UpdateStatusesAsync's scope
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CodeArenaDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var userIds = await db.Users
                .Where(u => u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var userId in userIds)
                await notificationService.CreateAsync(userId, type, title, body);

            logger.LogInformation("Sent {Type} notification to {Count} users", type, userIds.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send competition notification {Type}", type);
        }
    }
}
