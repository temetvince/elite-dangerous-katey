using Microsoft.Extensions.Logging;

namespace Katey.Journal;

/// <summary>The journal library's log messages, defined once as source-generated delegates.</summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Watching the journal folder.")]
    internal static partial void Started(ILogger logger);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Stopped watching the journal folder.")]
    internal static partial void Stopped(ILogger logger);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "The journal folder does not exist.")]
    internal static partial void FolderMissing(ILogger logger);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "No journal files in the folder yet.")]
    internal static partial void NoJournals(ILogger logger);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Information, Message = "Replayed {FileCount} journal file(s): {Lines} line(s) in {Seconds} s, {Skipped} unreadable.")]
    internal static partial void CaughtUp(ILogger logger, int fileCount, int lines, int skipped, double seconds);

    [LoggerMessage(EventId = 1014, Level = LogLevel.Warning, Message = "The previous journal replay did not stop within 30 seconds; its last lines may have reached the trackers after the new folder's replay.")]
    internal static partial void StopTimedOut(ILogger logger);

    [LoggerMessage(EventId = 1013, Level = LogLevel.Warning, Message = "The game wrote {File} in a form that could not be read; {Count} such write(s) so far.")]
    internal static partial void LiveFileUnreadable(ILogger logger, string file, int count);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Information, Message = "Following journal {File}.")]
    internal static partial void SwitchedJournal(ILogger logger, string file);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Debug, Message = "Skipped an unreadable line in {File}.")]
    internal static partial void UnreadableLine(ILogger logger, string file);

    [LoggerMessage(EventId = 1008, Level = LogLevel.Error, Message = "The journal monitor stopped after an error.")]
    internal static partial void LoopFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1009, Level = LogLevel.Information, Message = "Backed up the journal folder: {Copied} copied, {Skipped} current, {Failed} failed.")]
    internal static partial void BackedUp(ILogger logger, int copied, int skipped, int failed);

    [LoggerMessage(EventId = 1010, Level = LogLevel.Warning, Message = "Could not back up {File}.")]
    internal static partial void BackupFileFailed(ILogger logger, string file, Exception exception);

    [LoggerMessage(EventId = 1011, Level = LogLevel.Warning, Message = "Journal {File} shrank from {Previous} to {Length} bytes: the game rewrote it and the lines it held are lost. Reading it from the start.")]
    internal static partial void JournalRewritten(ILogger logger, string file, long previous, long length);

    [LoggerMessage(EventId = 1012, Level = LogLevel.Information, Message = "Kept the earlier copy of {File} as {Kept}: the source is now shorter.")]
    internal static partial void BackupPreserved(ILogger logger, string file, string kept);
}
