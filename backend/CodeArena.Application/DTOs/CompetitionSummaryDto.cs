namespace CodeArena.Application.DTOs;

public record CompetitionSummaryDto(
    Guid Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    int ProblemCount
);
