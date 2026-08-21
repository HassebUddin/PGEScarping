namespace PGEScarping.Dto;

public sealed class ScrapeResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public string OutputFilePath { get; init; } = "";
    public int RecordCount { get; init; }
}
