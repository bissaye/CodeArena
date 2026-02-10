using CodeArena.Domain.Entities;

namespace CodeArena.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user, out DateTime expiresAt);
}
