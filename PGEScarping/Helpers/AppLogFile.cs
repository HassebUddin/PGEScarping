namespace PGEScarping.Helpers;

public sealed class AppLogFile
{
    public string FilePath { get; }

    public AppLogFile()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(directory);
        FilePath = Path.Combine(directory, $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
    }

    private readonly object _writeLock = new();

    public void Append(string line)
    {
        lock (_writeLock)
        {
            File.AppendAllText(FilePath, line + Environment.NewLine);
        }
    }
}
