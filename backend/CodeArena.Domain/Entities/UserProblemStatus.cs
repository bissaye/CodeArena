namespace CodeArena.Domain.Entities;

public class UserProblemStatus
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid ProblemId { get; set; }
    public Problem? Problem { get; set; }

    public bool Solved { get; set; } = false;
    public int AttemptCount { get; set; } = 0;
    public DateTime? LastAttemptAt { get; set; }
}
