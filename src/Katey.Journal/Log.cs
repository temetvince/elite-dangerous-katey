using Microsoft.Extensions.Logging;

namespace Katey.Journal;

/// <summary>The journal library's log messages, defined once as source-generated delegates.</summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Watching journal folder {Folder}.")]
    internal static partial void Started(ILogger logger, string folder);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Stopped watching the journal folder.")]
    internal static partial void Stopped(ILogger logger);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Journal folder {Folder} does not exist.")]
    internal static partial void FolderMissing(ILogger logger, string folder);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "No journal files in {Folder} yet.")]
    internal static partial void NoJournals(ILogger logger, string folder);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Information, Message = "Replayed {FileCount} journal file(s).")]
    internal static partial void CaughtUp(ILogger logger, int fileCount);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Information, Message = "Following journal {Path}.")]
    internal static partial void SwitchedJournal(ILogger logger, string path);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Debug, Message = "Skipped an unreadable line in {Path}.")]
    internal static partial void UnreadableLine(ILogger logger, string path);

    [LoggerMessage(EventId = 1008, Level = LogLevel.Error, Message = "The journal monitor stopped after an error.")]
    internal static partial void LoopFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1009, Level = LogLevel.Information, Message = "Backed up {Source} to {Destination}: {Copied} copied, {Skipped} current, {Failed} failed.")]
    internal static partial void BackedUp(ILogger logger, string source, string destination, int copied, int skipped, int failed);

    [LoggerMessage(EventId = 1010, Level = LogLevel.Warning, Message = "Could not back up {File}.")]
    internal static partial void BackupFileFailed(ILogger logger, string file, Exception exception);

    [LoggerMessage(EventId = 1011, Level = LogLevel.Warning, Message = "Journal {Path} shrank from {Previous} to {Length} bytes: the game rewrote it and the lines it held are lost. Reading it from the start.")]
    internal static partial void JournalRewritten(ILogger logger, string path, long previous, long length);

    [LoggerMessage(EventId = 1012, Level = LogLevel.Information, Message = "Kept the earlier copy of {File} as {Kept}: the source is now shorter.")]
    internal static partial void BackupPreserved(ILogger logger, string file, string kept);
}
