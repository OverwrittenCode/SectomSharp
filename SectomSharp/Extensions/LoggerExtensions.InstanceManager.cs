using Microsoft.Extensions.Logging;

namespace SectomSharp.Extensions;

internal static partial class LoggerExtensions
{
    [LoggerMessage(100, LogLevel.Debug, "Instance initialized: inactivity timer started")]
    public static partial void InstanceTimerStarted(this ILogger logger);

    [LoggerMessage(102, LogLevel.Debug, "Instance inactivity timeout reached: automatic cleanup triggered")]
    public static partial void InstanceInactivityTimeoutTriggered(this ILogger logger);

    [LoggerMessage(103, LogLevel.Debug, "Instance hard timeout reached: automatic cleanup triggered")]
    public static partial void InstanceHardTimeoutTriggered(this ILogger logger);

    [LoggerMessage(104, LogLevel.Debug, "Instance received activity: inactivity timer extended")]
    public static partial void InstanceInactivityTimerExtended(this ILogger logger);

    [LoggerMessage(105, LogLevel.Debug, "Instance completed: manual cleanup triggered")]
    public static partial void InstanceCompleted(this ILogger logger);

    [LoggerMessage(106, LogLevel.Error, "Instance unhandled exception")]
    public static partial void InstanceUnhandledException(this ILogger logger, Exception ex);
}
