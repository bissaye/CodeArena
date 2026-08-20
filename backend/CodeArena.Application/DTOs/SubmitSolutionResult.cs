namespace CodeArena.Application.DTOs;

public record SubmitSolutionResult(
    string Status,
    string Message,
    int? PointsEarned
);
