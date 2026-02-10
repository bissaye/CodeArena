namespace CodeArena.Application.Interfaces;

public interface IMarkdownSanitizerService
{
    /// <summary>
    /// Renders Markdown to safe HTML (raw HTML tags stripped via Markdig DisableHtml).
    /// </summary>
    string Sanitize(string markdownBody);
}
