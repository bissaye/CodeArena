using CodeArena.Domain.Enums;

namespace CodeArena.Domain.Entities;

public class Submission
{
    public Guid Id { get; set; }
    public Guid ProblemId { get; set; }
    public Problem? Problem { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string ResultFileUrl { get; set; } = string.Empty;
    public string? SourceFileUrl { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
    public bool IsFirstAccepted { get; set; } = false;
}
