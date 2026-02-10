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
    private readonly ILogger<AuthService> _logger;

    public AuthService(IAppDbContext db, IJwtService jwt, IPasswordHasher hasher, ILogger<AuthService> logger)
    {
        _db = db;
        _jwt = jwt;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var usernameExists = await _db.Users
            .AnyAsync(u => u.Username == request.Username, ct);

        if (usernameExists)
            throw new ConflictException($"Le pseudonyme '{request.Username}' est déjà utilisé.");

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailExists = await _db.Users
                .AnyAsync(u => u.Email == request.Email, ct);
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
        await _db.SaveChangesAsync(ct);

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
            ?? throw new NotFoundException($"Utilisateur introuvable.");

        if (!_hasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("Mot de passe actuel incorrect.");

        if (request.NewPassword == request.CurrentPassword)
            throw new ArgumentException("Le nouveau mot de passe doit être différent de l'ancien.");

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Mot de passe changé : {UserId}", userId);
    }
}
