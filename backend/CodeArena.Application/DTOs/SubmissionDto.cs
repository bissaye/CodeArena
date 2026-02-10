namespace CodeArena.Application.DTOs;

public record SubmissionDto(
    Guid Id,
    DateTime SubmittedAt,
    string Status,
    bool IsFirstAccepted
);
