using System.ComponentModel;
using CodeArena.Application.Interfaces;
using CodeArena.Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeArena.Infrastructure.Jobs;

[DisplayName("Competition Status Update")]
public class CompetitionStatusJob(
    IAppDbContext db,
    IBackgroundJobClient backgroundJobClient,
    ILogger<CompetitionStatusJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var competitions = await db.Competitions
            .Where(c => c.Status == CompetitionStatus.Upcoming || c.Status == CompetitionStatus.Ongoing)
            .ToListAsync(ct);

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

        if (updated > 0)
            await db.SaveChangesAsync(ct);

        // Enqueue a single "notify all" job per notification type — avoids N*users jobs at schedule time
        foreach (var (type, title, body) in pendingNotifications)
            backgroundJobClient.Enqueue<CompetitionStatusJob>(j =>
                j.NotifyAllUsersAsync(type, title, body, CancellationToken.None));
    }

    [DisplayName("Competition Notify All — {0}")]
    public async Task NotifyAllUsersAsync(NotificationType type, string title, string body, CancellationToken ct = default)
    {
        var userIds = await db.Users
            .Where(u => u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(ct);

        // Enqueue one notification creation job per user (with retry on failure)
        foreach (var userId in userIds)
            backgroundJobClient.Enqueue<INotificationService>(s =>
                s.CreateAsync(userId, type, title, body, CancellationToken.None));

        logger.LogInformation("Enqueued {Type} notification for {Count} users", type, userIds.Count);
    }
}
