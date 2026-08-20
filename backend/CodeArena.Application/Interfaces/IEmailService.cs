namespace CodeArena.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string username, string token, CancellationToken ct = default);
    Task SendPasswordResetAsync(string toEmail, string username, string token, CancellationToken ct = default);
    Task SendWelcomeAsync(string toEmail, string username, CancellationToken ct = default);
}
