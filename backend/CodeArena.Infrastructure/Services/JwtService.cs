using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CodeArena.Application.Interfaces;
using CodeArena.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CodeArena.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly string _secret;
    private readonly int _expiryHours;

    public JwtService(IConfiguration configuration)
    {
        _secret = configuration["JWT_SECRET"]
            ?? throw new InvalidOperationException("JWT_SECRET is not configured.");
        _expiryHours = int.TryParse(configuration["JWT_EXPIRY_HOURS"], out var h) ? h : 24;
    }

    public string GenerateToken(User user, out DateTime expiresAt)
    {
        expiresAt = DateTime.UtcNow.AddHours(_expiryHours);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim("role", user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
