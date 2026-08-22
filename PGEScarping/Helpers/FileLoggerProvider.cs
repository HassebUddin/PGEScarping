using Microsoft.Extensions.Logging;

namespace PGEScarping.Helpers;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly AppLogFile _logFile;

    public FileLoggerProvider(AppLogFile logFile)
    {
        _logFile = logFile;
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(_logFile, categoryName);

    public void Dispose() { }

    private sealed class FileLogger : ILogger
    {
        private readonly AppLogFile _logFile;
        private readonly string _categoryName;

        public FileLogger(AppLogFile logFile, string categoryName)
        {
            _logFile = logFile;
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var line = $"[{DateTime.Now:HH:mm:ss}] [{logLevel}] {_categoryName}: {formatter(state, exception)}";
            if (exception is not null)
                line += Environment.NewLine + exception;

            _logFile.Append(line);
        }
    }
}
