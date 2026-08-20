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
        var now = DateTime.UtcNow;

        var competitions = await db.Competitions
            .Where(c => c.Status == CompetitionStatus.Upcoming || c.Status == CompetitionStatus.Ongoing)
            .ToListAsync(ct);

        int updated = 0;
        foreach (var comp in competitions)
        {
            var endDate = comp.StartDate.Add(comp.Duration);

            if (comp.Status == CompetitionStatus.Upcoming && now >= comp.StartDate)
            {
                comp.Status = CompetitionStatus.Ongoing;
                updated++;
                logger.LogInformation("Competition {Id} ({Name}) → Ongoing", comp.Id, comp.Name);
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
}
