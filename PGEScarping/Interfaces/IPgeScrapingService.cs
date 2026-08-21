using PGEScarping.Dto;

namespace PGEScarping.Interfaces;

public interface IPgeScrapingService
{
    Task<PgeScrapeResult> RunAsync(IProgress<string> progress, CancellationToken cancellationToken = default);
}
