using Microsoft.Web.WebView2.WinForms;
using PGEScarping.Dto;
using PGEScarping.Enums;

namespace PGEScarping.Interfaces;

public interface IScrapingModule
{
    ScrapingSourceType SourceType { get; }
    string DisplayName { get; }
    string Description { get; }
    string IconGlyph { get; }
    bool IsAvailable { get; }

    Task<ScrapeResult> RunAsync(
        WebView2 browser,
        IProgress<string> progress,
        Func<string, Task<string?>> promptForInputAsync,
        string? accountNumberOverride = null,
        CancellationToken cancellationToken = default);
}
