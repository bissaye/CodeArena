using CodeArena.Application.DTOs;
using CodeArena.Application.Exceptions;
using CodeArena.Application.Interfaces;
using CodeArena.Domain.Entities;
using CodeArena.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CodeArena.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAppDbContext _db;
    private readonly IJwtService _jwt;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailService _email;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAppDbContext db,
        IJwtService jwt,
        IPasswordHasher hasher,
        IEmailService email,
        ILogger<AuthService> logger)
    {
        _db = db;
        _jwt = jwt;
        _hasher = hasher;
        _email = email;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var usernameExists = await _db.Users.AnyAsync(u => u.Username == request.Username, ct);
        if (usernameExists)
            throw new ConflictException($"Le pseudonyme '{request.Username}' est déjà utilisé.");

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailExists = await _db.Users.AnyAsync(u => u.Email == request.Email, ct);
            if (emailExists)
                throw new ConflictException($"L'adresse email '{request.Email}' est déjà associée à un compte.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = _hasher.Hash(request.Password),
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Country = request.Country,
            Region = request.Region,
            School = request.School,
            Role = UserRole.Participant,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
        };

        _db.Users.Add(user);

        // Envoi email de vérification si email fourni
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            var verification = CreateEmailVerification(user.Id);
            _db.EmailVerifications.Add(verification);
            await _db.SaveChangesAsync(ct);

            _ = SendVerificationEmailSafe(user.Email, user.Username, verification.Token);
        }
        else
        {
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Nouvel utilisateur inscrit : {Username}", user.Username);

        var token = _jwt.GenerateToken(user, out var expiresAt);
        return new AuthResponse(token, user.Username, user.Role.ToString(), expiresAt);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive, ct);

        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Tentative de connexion échouée pour : {Username}", request.Username);
            throw new UnauthorizedException("Identifiants incorrects.");
        }

        _logger.LogInformation("Connexion réussie : {Username}", user.Username);

        var token = _jwt.GenerateToken(user, out var expiresAt);
        return new AuthResponse(token, user.Username, user.Role.ToString(), expiresAt);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("Utilisateur introuvable.");

        if (!_hasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("Mot de passe actuel incorrect.");

        if (request.NewPassword == request.CurrentPassword)
            throw new BadRequestException("Le nouveau mot de passe doit être différent de l'ancien.");

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Mot de passe changé : {UserId}", userId);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, ct);

        // Réponse identique que l'utilisateur existe ou non (anti-enumeration)
        if (user is null)
        {
            _logger.LogInformation("Demande reset pour email inconnu (ignorée silencieusement)");
            return;
        }

        // Invalider les tokens précédents non utilisés
        var oldTokens = await _db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);
        foreach (var old in oldTokens)
            old.ExpiresAt = DateTime.UtcNow; // invalider immédiatement

        var resetToken = CreatePasswordResetToken(user.Id);
        _db.PasswordResetTokens.Add(resetToken);
        user.PasswordResetRequestedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _ = SendPasswordResetEmailSafe(user.Email!, user.Username, resetToken.Token);
        _logger.LogInformation("Token de réinitialisation créé pour {Username}", user.Username);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var resetToken = await _db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.Token, ct);

        if (resetToken is null || resetToken.UsedAt.HasValue || resetToken.ExpiresAt < DateTime.UtcNow)
            throw new BadRequestException("Ce lien de réinitialisation est invalide ou a expiré.");

        resetToken.UsedAt = DateTime.UtcNow;
        resetToken.User.PasswordHash = _hasher.Hash(request.NewPassword);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Mot de passe réinitialisé via token pour userId={UserId}", resetToken.UserId);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default)
    {
        var verification = await _db.EmailVerifications
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Token == request.Token, ct);

        if (verification is null || verification.UsedAt.HasValue || verification.ExpiresAt < DateTime.UtcNow)
            throw new BadRequestException("Ce lien de vérification est invalide ou a expiré.");

        verification.UsedAt = DateTime.UtcNow;
        verification.User.EmailVerifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Email vérifié pour userId={UserId}", verification.UserId);
    }

    public async Task ResendVerificationAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("Utilisateur introuvable.");

        if (user.EmailVerifiedAt.HasValue)
            throw new BadRequestException("Cet email est déjà vérifié.");

        if (string.IsNullOrWhiteSpace(user.Email))
            throw new BadRequestException("Aucune adresse email associée à ce compte.");

        // Invalider les anciens tokens non utilisés
        var oldTokens = await _db.EmailVerifications
            .Where(e => e.UserId == userId && e.UsedAt == null && e.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);
        foreach (var old in oldTokens)
            old.ExpiresAt = DateTime.UtcNow;

        var verification = CreateEmailVerification(userId);
        _db.EmailVerifications.Add(verification);
        await _db.SaveChangesAsync(ct);

        _ = SendVerificationEmailSafe(user.Email, user.Username, verification.Token);
        _logger.LogInformation("Email de vérification renvoyé pour {Username}", user.Username);
    }

    // --- Helpers privés ---

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

    // Fire-and-forget : l'envoi d'email ne bloque pas la réponse HTTP
    private async Task SendVerificationEmailSafe(string email, string username, string token)
    {
        try { await _email.SendEmailVerificationAsync(email, username, token); }
        catch (Exception ex) { _logger.LogError(ex, "Échec envoi email vérification à {Email}", email); }
    }

    private async Task SendPasswordResetEmailSafe(string email, string username, string token)
    {
        try { await _email.SendPasswordResetAsync(email, username, token); }
        catch (Exception ex) { _logger.LogError(ex, "Échec envoi email reset à {Email}", email); }
    }
}
