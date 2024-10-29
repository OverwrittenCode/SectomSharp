using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Serilog;
using Serilog.Events;

namespace SectomSharp.Services;

internal sealed class LoggingService
{
    private readonly ILogger _logger;

    public LoggingService(DiscordSocketClient client, InteractionService command, ILogger logger)
    {
        client.Log += LogAsync;
        command.Log += LogAsync;
        _logger = logger;
    }

    private Task LogAsync(LogMessage message)
    {
        LogEventLevel severity = message.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Verbose => LogEventLevel.Verbose,
            LogSeverity.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };

        _logger.Write(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);

        return Task.CompletedTask;
    }
}
