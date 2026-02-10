namespace CodeArena.Application.DTOs;

public record UserActivityDto(
    Guid ProblemId,
    string ProblemTitle,
    Guid CompetitionId,
    string CompetitionName,
    string Status,
    DateTime SubmittedAt
);
