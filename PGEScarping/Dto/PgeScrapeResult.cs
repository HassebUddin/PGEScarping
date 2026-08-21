using PGEScarping.Models;

namespace PGEScarping.Dto;

public sealed class PgeScrapeResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public string OutputFilePath { get; init; } = "";
    public List<PgeBillRecord> Bills { get; init; } = [];
}
