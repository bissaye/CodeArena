using CodeArena.Domain.Enums;

namespace CodeArena.Domain.Entities;

public class Competition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public TimeSpan Duration { get; set; }
    public CompetitionStatus Status { get; set; } = CompetitionStatus.Draft;

    public Guid CreatedByUserId { get; set; }
    public User? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? LastModifiedByUserId { get; set; }
    public User? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }

    public ICollection<Problem> Problems { get; set; } = [];

    public DateTime EndDate => StartDate.Add(Duration);

    /// Tracks whether the "starting in 1h" reminder notification was already sent.
    public DateTime? StartReminderSentAt { get; set; }
}
