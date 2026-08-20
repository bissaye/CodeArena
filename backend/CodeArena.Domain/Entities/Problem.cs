namespace CodeArena.Domain.Entities;

public class Problem
{
    public Guid Id { get; set; }
    public Guid CompetitionId { get; set; }
    public Competition? Competition { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int Points { get; set; }

    public string InputFileUrl { get; set; } = string.Empty;
    public string OutputFileUrl { get; set; } = string.Empty;

    public Guid CreatedByUserId { get; set; }
    public User? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? LastModifiedByUserId { get; set; }
    public User? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }

    public ICollection<Submission> Submissions { get; set; } = [];
    public ICollection<UserProblemStatus> UserStatuses { get; set; } = [];
}
