using Microsoft.Extensions.Logging;

namespace Katey.App;

/// <summary>The application layer's log messages, defined once as source-generated delegates.</summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 3000, Level = LogLevel.Warning, Message = "Could not read the saved state; starting empty.")]
    internal static partial void StateLoadFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Error, Message = "Could not save the state.")]
    internal static partial void StateSaveFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Information, Message = "Loaded {EntryCount} plan entry(ies) and {EngineerCount} selected engineer(s).")]
    internal static partial void StateLoaded(ILogger logger, int entryCount, int engineerCount);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Information, Message = "Imported {ModuleCount} module(s) from the build \"{Ship}\".")]
    internal static partial void BuildImported(ILogger logger, int moduleCount, string ship);

    [LoggerMessage(EventId = 3004, Level = LogLevel.Information, Message = "Journal folder changed.")]
    internal static partial void JournalFolderChosen(ILogger logger);

    [LoggerMessage(EventId = 3005, Level = LogLevel.Debug, Message = "Opened page {Kind} \"{Id}\".")]
    internal static partial void PageOpened(ILogger logger, Navigation.PageKind kind, string id);

    [LoggerMessage(EventId = 3015, Level = LogLevel.Information, Message = "The game's engineering reached the plan entry {Blueprint} grade {Grade} with experimental {Experimental}; it left the plan.")]
    internal static partial void PlanEntryCompleted(ILogger logger, string blueprint, int grade, string experimental);

    [LoggerMessage(EventId = 3006, Level = LogLevel.Information, Message = "Live rail moved to its own window: {Detached}.")]
    internal static partial void RailMoved(ILogger logger, bool detached);

    [LoggerMessage(EventId = 3008, Level = LogLevel.Information, Message = "Asked Spansh for a route from {Source} to {Destination}.")]
    internal static partial void PlotSubmitted(ILogger logger, string source, string destination);

    [LoggerMessage(EventId = 3009, Level = LogLevel.Information, Message = "Spansh plotted {Jumps} jump(s) from {Source} to {Destination} in {ElapsedMs} ms.")]
    internal static partial void PlotReceived(ILogger logger, string source, string destination, int jumps, long elapsedMs);

    [LoggerMessage(EventId = 3010, Level = LogLevel.Warning, Message = "Spansh refused the route from {Source} to {Destination}: {Reason}.")]
    internal static partial void PlotRefused(ILogger logger, string source, string destination, string reason);

    [LoggerMessage(EventId = 3011, Level = LogLevel.Warning, Message = "The plot from {Source} to {Destination} failed.")]
    internal static partial void PlotFailed(ILogger logger, string source, string destination, Exception exception);

    [LoggerMessage(EventId = 3012, Level = LogLevel.Error, Message = "The journal backup failed.")]
    internal static partial void BackupFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3013, Level = LogLevel.Information, Message = "Backed up the journal folder on close: {Copied} file(s) copied, {Failed} failed.")]
    internal static partial void BackedUpOnClose(ILogger logger, int copied, int failed);

    [LoggerMessage(EventId = 3014, Level = LogLevel.Information, Message = "Saved data moved to another folder; a saved state was already there: {Found}.")]
    internal static partial void StateMoved(ILogger logger, bool found);

    [LoggerMessage(EventId = 3016, Level = LogLevel.Information, Message = "Outgoing connections: routes {Routes}, system bodies {Bodies}, release check {Updates}.")]
    internal static partial void NetworkAccessChanged(ILogger logger, bool routes, bool bodies, bool updates);

    [LoggerMessage(EventId = 3017, Level = LogLevel.Warning, Message = "Looking up system {SystemAddress} failed.")]
    internal static partial void SystemLookupFailed(ILogger logger, long systemAddress, Exception exception);

    [LoggerMessage(EventId = 3018, Level = LogLevel.Debug, Message = "Looked up system {SystemAddress}: {Bodies} bodies in {ElapsedMs} ms.")]
    internal static partial void SystemLookedUp(ILogger logger, long systemAddress, int bodies, long elapsedMs);

    [LoggerMessage(EventId = 3019, Level = LogLevel.Information, Message = "Backup folder {State}.")]
    internal static partial void BackupFolderSet(ILogger logger, string state);

    [LoggerMessage(EventId = 3020, Level = LogLevel.Information, Message = "Rail body view set to {View}.")]
    internal static partial void RailViewChanged(ILogger logger, string view);

    [LoggerMessage(EventId = 3021, Level = LogLevel.Information, Message = "Rail body filter set to {Filter}.")]
    internal static partial void RailFilterChanged(ILogger logger, string filter);

    [LoggerMessage(EventId = 3022, Level = LogLevel.Debug, Message = "Rail shows the {Section}: {Reason}.")]
    internal static partial void RailSectionChanged(ILogger logger, string section, string reason);

    [LoggerMessage(EventId = 3023, Level = LogLevel.Debug, Message = "Not in the tables: {Kind} {Symbol}.")]
    internal static partial void UnknownSymbol(ILogger logger, string kind, string symbol);

    [LoggerMessage(EventId = 3024, Level = LogLevel.Information, Message = "Language changed from {Before} to {After}.")]
    internal static partial void LanguageChanged(ILogger logger, string before, string after);

    [LoggerMessage(EventId = 3025, Level = LogLevel.Debug, Message = "GitHub named release {Release} in {ElapsedMs} ms.")]
    internal static partial void ReleaseLookedUp(ILogger logger, string release, long elapsedMs);

    [LoggerMessage(EventId = 3026, Level = LogLevel.Information, Message = "Release check: running {Current}, latest {Latest}.")]
    internal static partial void UpdateChecked(ILogger logger, Version current, Version latest);

    [LoggerMessage(EventId = 3027, Level = LogLevel.Warning, Message = "The release check failed.")]
    internal static partial void UpdateCheckFailed(ILogger logger, Exception exception);
}
