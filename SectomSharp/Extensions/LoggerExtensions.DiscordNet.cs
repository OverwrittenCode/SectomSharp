using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace SectomSharp.Extensions;

internal static partial class LoggerExtensions
{
    private const string DiscordNetMessageFormat = "[{Source}] {Message}";

    [LoggerMessage(1, LogLevel.Critical, DiscordNetMessageFormat)]
    public static partial void DiscordNetCritical(this ILogger logger, string source, string message, Exception? ex = null);

    [LoggerMessage(2, LogLevel.Error, DiscordNetMessageFormat)]
    public static partial void DiscordNetError(this ILogger logger, string source, string message, Exception? ex = null);

    [LoggerMessage(3, LogLevel.Warning, DiscordNetMessageFormat)]
    public static partial void DiscordNetWarning(this ILogger logger, string source, string message, Exception? ex = null);

    [LoggerMessage(4, LogLevel.Information, DiscordNetMessageFormat)]
    public static partial void DiscordNetInformation(this ILogger logger, string source, string message, Exception? ex = null);

    [LoggerMessage(5, LogLevel.Debug, DiscordNetMessageFormat)]
    public static partial void DiscordNetDebug(this ILogger logger, string source, string message, Exception? ex = null);

    [LoggerMessage(6, LogLevel.Trace, DiscordNetMessageFormat)]
    public static partial void DiscordNetVerbose(this ILogger logger, string source, string message, Exception? ex = null);

    [LoggerMessage(7, LogLevel.Error, "{Message}")]
    public static partial void DiscordNetUnhandledException(this ILogger logger, string message, Exception ex);

    [LoggerMessage(8, LogLevel.Information, "{Error} {ErrorReason}")]
    public static partial void DiscordNetInteractionCommandFailed(this ILogger logger, InteractionCommandError error, string errorReason);
}
