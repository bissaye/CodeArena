using CodeArena.Application.DTOs;
using CodeArena.Application.Exceptions;
using CodeArena.Application.Interfaces;
using CodeArena.Domain.Entities;
using CodeArena.Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeArena.Application.Services;

public class AuthService(
    IAppDbContext db,
    IJwtService jwt,
    IPasswordHasher hasher,
    IBackgroundJobClient backgroundJobClient,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var usernameExists = await db.Users.AnyAsync(u => u.Username == request.Username, ct);
        if (usernameExists)
            throw new ConflictException($"Le pseudonyme '{request.Username}' est déjà utilisé.");

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailExists = await db.Users.AnyAsync(u => u.Email == request.Email, ct);
            if (emailExists)
                throw new ConflictException($"L'adresse email '{request.Email}' est déjà associée à un compte.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = hasher.Hash(request.Password),
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Country = request.Country,
            Region = request.Region,
            School = request.School,
            Role = UserRole.Participant,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
        };

        db.Users.Add(user);

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            var verification = CreateEmailVerification(user.Id);
            db.EmailVerifications.Add(verification);
            await db.SaveChangesAsync(ct);

            // Enqueue with retry — email send should not block registration
            var capturedEmail = user.Email;
            var capturedUsername = user.Username;
            var capturedToken = verification.Token;
            backgroundJobClient.Enqueue<IEmailService>(s =>
                s.SendEmailVerificationAsync(capturedEmail, capturedUsername, capturedToken, CancellationToken.None));
        }
        else
        {
            await db.SaveChangesAsync(ct);
        }

        logger.LogInformation("Nouvel utilisateur inscrit : {Username}", user.Username);

        var token = jwt.GenerateToken(user, out var expiresAt);
        return new AuthResponse(token, user.Username, user.Role.ToString(), expiresAt);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive, ct);

        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Tentative de connexion échouée pour : {Username}", request.Username);
            throw new UnauthorizedException("Identifiants incorrects.");
        }

        logger.LogInformation("Connexion réussie : {Username}", user.Username);

        var token = jwt.GenerateToken(user, out var expiresAt);
        return new AuthResponse(token, user.Username, user.Role.ToString(), expiresAt);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("Utilisateur introuvable.");

        if (!hasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("Mot de passe actuel incorrect.");

        if (request.NewPassword == request.CurrentPassword)
            throw new BadRequestException("Le nouveau mot de passe doit être différent de l'ancien.");

        user.PasswordHash = hasher.Hash(request.NewPassword);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Mot de passe changé : {UserId}", userId);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, ct);

        // Réponse identique que l'utilisateur existe ou non (anti-enumeration)
        if (user is null)
        {
            logger.LogInformation("Demande reset pour email inconnu (ignorée silencieusement)");
            return;
        }

        var oldTokens = await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);
        foreach (var old in oldTokens)
            old.ExpiresAt = DateTime.UtcNow;

        var resetToken = CreatePasswordResetToken(user.Id);
        db.PasswordResetTokens.Add(resetToken);
        user.PasswordResetRequestedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var capturedEmail = user.Email!;
        var capturedUsername = user.Username;
        var capturedToken = resetToken.Token;
        backgroundJobClient.Enqueue<IEmailService>(s =>
            s.SendPasswordResetAsync(capturedEmail, capturedUsername, capturedToken, CancellationToken.None));

        logger.LogInformation("Token de réinitialisation créé pour {Username}", user.Username);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var resetToken = await db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.Token, ct);

        if (resetToken is null || resetToken.UsedAt.HasValue || resetToken.ExpiresAt < DateTime.UtcNow)
            throw new BadRequestException("Ce lien de réinitialisation est invalide ou a expiré.");

        resetToken.UsedAt = DateTime.UtcNow;
        resetToken.User.PasswordHash = hasher.Hash(request.NewPassword);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Mot de passe réinitialisé via token pour userId={UserId}", resetToken.UserId);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default)
    {
        var verification = await db.EmailVerifications
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Token == request.Token, ct);

        if (verification is null || verification.UsedAt.HasValue || verification.ExpiresAt < DateTime.UtcNow)
            throw new BadRequestException("Ce lien de vérification est invalide ou a expiré.");

        verification.UsedAt = DateTime.UtcNow;
        verification.User.EmailVerifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Email vérifié pour userId={UserId}", verification.UserId);
    }

    public async Task ResendVerificationAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("Utilisateur introuvable.");

        if (user.EmailVerifiedAt.HasValue)
            throw new BadRequestException("Cet email est déjà vérifié.");

        if (string.IsNullOrWhiteSpace(user.Email))
            throw new BadRequestException("Aucune adresse email associée à ce compte.");

        var oldTokens = await db.EmailVerifications
            .Where(e => e.UserId == userId && e.UsedAt == null && e.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);
        foreach (var old in oldTokens)
            old.ExpiresAt = DateTime.UtcNow;

        var verification = CreateEmailVerification(userId);
        db.EmailVerifications.Add(verification);
        await db.SaveChangesAsync(ct);

        var capturedEmail = user.Email;
        var capturedUsername = user.Username;
        var capturedToken = verification.Token;
        backgroundJobClient.Enqueue<IEmailService>(s =>
            s.SendEmailVerificationAsync(capturedEmail, capturedUsername, capturedToken, CancellationToken.None));

        logger.LogInformation("Email de vérification renvoyé pour {Username}", user.Username);
    }

    private static EmailVerification CreateEmailVerification(Guid userId) => new()
    {
        UserId = userId,
        Token = Guid.NewGuid().ToString("N"),
        ExpiresAt = DateTime.UtcNow.AddHours(1),
        CreatedAt = DateTime.UtcNow,
    };

    private static PasswordResetToken CreatePasswordResetToken(Guid userId) => new()
    {
        UserId = userId,
        Token = Guid.NewGuid().ToString("N"),
        ExpiresAt = DateTime.UtcNow.AddHours(1),
        CreatedAt = DateTime.UtcNow,
    };
}
