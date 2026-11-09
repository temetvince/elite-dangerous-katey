using Microsoft.Extensions.Logging;

namespace Katey.App.Logging;

/// <summary>
/// Writes log lines to one text file, so a run can be understood after the fact and a crash leaves a trace.
/// Lines below the minimum level are dropped. When the file grows past the roll-over size it is renamed with
/// <c>.old</c> appended, replacing the previous one, and a fresh file begins; two files are the most kept.
/// A write that fails, such as on a read-only folder, is dropped without disturbing the caller. Every line
/// passes through the scrubber given before it is written, exception text included.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly Lock _gate = new();
    private readonly long _rollOverBytes;
    private readonly Func<string, string> _scrub;

    /// <summary>Creates the provider.</summary>
    /// <param name="path">The log file. Its folder is created on the first write.</param>
    /// <param name="minimum">The lowest level written; <see cref="LogLevel.Information"/> keeps the fine-grained trail out of the file.</param>
    /// <param name="rollOverBytes">The size past which the file is rolled over; at least 1 KB.</param>
    /// <param name="scrub">Applied to every line before it is written, such as <see cref="LogScrubber.Scrub"/>; <see langword="null"/> writes lines as they are.</param>
    public FileLoggerProvider(string path, LogLevel minimum = LogLevel.Information, long rollOverBytes = 1_000_000, Func<string, string>? scrub = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Path = System.IO.Path.GetFullPath(path);
        Minimum = minimum;
        _rollOverBytes = Math.Max(rollOverBytes, 1024);
        _scrub = scrub ?? (text => text);
    }

    /// <summary>Gets the log file's full path.</summary>
    public string Path { get; }

    /// <summary>Gets the lowest level written to the file.</summary>
    public LogLevel Minimum { get; }

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName ?? "");

    /// <summary>Writes an exception that is ending the process, at <see cref="LogLevel.Critical"/>, whatever the minimum level.</summary>
    /// <param name="exception">The exception, or <see langword="null"/> for an unknown failure.</param>
    public void WriteCrash(Exception? exception)
        => Write(LogLevel.Critical, "Katey", "The process is ending on an unhandled exception.", exception);

    /// <inheritdoc/>
    public void Dispose()
    {
        // Nothing is held open between writes.
    }

    private void Write(LogLevel level, string category, string message, Exception? exception)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} [{level}] {category}: {message}{Environment.NewLine}";
        if (exception is not null)
        {
            line += exception + Environment.NewLine;
        }

        line = _scrub(line);

        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
                if (File.Exists(Path) && new FileInfo(Path).Length > _rollOverBytes)
                {
                    File.Move(Path, Path + ".old", overwrite: true);
                }

                File.AppendAllText(Path, line);
            }
            catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
            {
                // The log must never take the application down with it.
            }
        }
    }

    private sealed class FileLogger(FileLoggerProvider owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= owner.Minimum;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            if (IsEnabled(logLevel))
            {
                owner.Write(logLevel, category, formatter(state, exception), exception);
            }
        }
    }
}
