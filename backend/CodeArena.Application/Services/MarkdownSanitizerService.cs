using CodeArena.Application.Interfaces;
using Markdig;

namespace CodeArena.Application.Services;

public class MarkdownSanitizerService : IMarkdownSanitizerService
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    public string Sanitize(string markdownBody)
    {
        if (string.IsNullOrWhiteSpace(markdownBody)) return markdownBody;
        // Renders to HTML with DisableHtml: raw HTML blocks/inlines are stripped.
        // Result is safe to store and display via innerHTML.
        return Markdown.ToHtml(markdownBody, Pipeline).TrimEnd();
    }
}
