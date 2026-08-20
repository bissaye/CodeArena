namespace CodeArena.Application.DTOs;

public record ProblemSummaryDto(
    Guid Id,
    string Title,
    int Points,
    int TotalSubmissions,
    int AcceptedSubmissions,
    bool? SolvedByCurrentUser
);
