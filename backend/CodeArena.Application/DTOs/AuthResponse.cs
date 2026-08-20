namespace CodeArena.Application.DTOs;

public record AuthResponse(
    string Token,
    string Username,
    string Role,
    DateTime ExpiresAt
);
