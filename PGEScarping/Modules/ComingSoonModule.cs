using Microsoft.Web.WebView2.WinForms;
using PGEScarping.Dto;
using PGEScarping.Enums;
using PGEScarping.Interfaces;

namespace PGEScarping.Modules;

public sealed class ComingSoonModule : IScrapingModule
{
    public ScrapingSourceType SourceType { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public string IconGlyph { get; }
    public bool IsAvailable => false;

    public ComingSoonModule(ScrapingSourceType sourceType, string displayName, string iconGlyph, string description)
    {
        SourceType = sourceType;
        DisplayName = displayName;
        IconGlyph = iconGlyph;
        Description = description;
    }

    public Task<ScrapeResult> RunAsync(
        WebView2 browser,
        IProgress<string> progress,
        Func<string, Task<string?>> promptForInputAsync,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException($"{DisplayName} is not available yet.");
}
