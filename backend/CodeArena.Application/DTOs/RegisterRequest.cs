namespace CodeArena.Application.DTOs;

public record RegisterRequest(
    string Username,
    string Password,
    string Country,
    string? Email,
    string? PhoneNumber,
    string? Region,
    string? School
);
