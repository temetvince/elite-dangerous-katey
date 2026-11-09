using Microsoft.Extensions.Logging;

namespace Katey.Ui;

/// <summary>Source-generated log messages for the desktop host.</summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 4001, Level = LogLevel.Information, Message = "Another Katey is already running; this one shows a notice and exits.")]
    internal static partial void AlreadyRunning(ILogger logger);
}
