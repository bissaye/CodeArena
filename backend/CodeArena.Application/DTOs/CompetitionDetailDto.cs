namespace CodeArena.Application.DTOs;

public record CompetitionDetailDto(
    Guid Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    IEnumerable<ProblemSummaryDto> Problems
);
