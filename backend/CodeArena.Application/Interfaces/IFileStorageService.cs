namespace CodeArena.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string originalFileName, string subfolder, CancellationToken ct = default);
    Task<string> SaveAvatarAsync(Stream imageStream, CancellationToken ct = default);
    Task<string> ReadFileContentAsync(string relativeFilePath, CancellationToken ct = default);
    string GetAbsolutePath(string relativeFilePath);
}
