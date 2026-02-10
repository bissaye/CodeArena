using CodeArena.Application.DTOs;

namespace CodeArena.Application.Interfaces;

public interface IUserService
{
    Task<UserProfileDto> GetProfileAsync(string username, CancellationToken ct = default);
    Task UpdateProfileAsync(string username, Guid requesterId, UpdateUserRequest request, CancellationToken ct = default);
    Task<string> UploadAvatarAsync(string username, Guid requesterId, Stream fileStream, string fileName, string contentType, long fileLength, CancellationToken ct = default);
    Task<IEnumerable<string>> GetRegionsAsync(CancellationToken ct = default);
    Task<IEnumerable<string>> GetSchoolsAsync(CancellationToken ct = default);
}
