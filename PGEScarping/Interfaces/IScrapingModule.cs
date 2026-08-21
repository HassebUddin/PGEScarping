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

    Task<ScrapeResult> RunAsync(IProgress<string> progress, CancellationToken cancellationToken = default);
}
