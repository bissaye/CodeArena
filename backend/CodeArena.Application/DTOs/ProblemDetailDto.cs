namespace CodeArena.Application.DTOs;

public record ProblemDetailDto(
    Guid Id,
    Guid CompetitionId,
    string CompetitionName,
    string CompetitionStatus,
    string Title,
    string Body,
    int Points,
    int TotalSubmissions,
    int AcceptedSubmissions,
    double AcceptanceRate,
    bool? SolvedByCurrentUser
);
