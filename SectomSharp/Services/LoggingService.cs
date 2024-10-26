using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;

namespace SectomSharp.Services;

internal sealed class LoggingService
{
    private string LogDirectory { get; }

    private string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");

    public LoggingService(DiscordSocketClient client, InteractionService command)
    {
        LogDirectory = "../logs";

        client.Log += LogAsync;
        command.Log += LogAsync;
        new EmbedBuilder().Build().ToJsonString();
    }

    private Task LogAsync(LogMessage message)
    {
        var text = message.Exception is CommandException cmdException
            ? $"[Command/{message.Severity}] {cmdException.Command.Aliases[0]} failed to execute in {cmdException.Context.Channel}: {cmdException}"
            : $"[General/{message.Severity}] {message}";

        if (message.Severity is LogSeverity.Error or LogSeverity.Critical)
        {
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }

            if (!File.Exists(LogFile))
            {
                File.Create(LogFile).Dispose();
            }

            File.AppendAllText(LogFile, $"{text}\n");
        }

        return Console.Out.WriteLineAsync(text);
    }
}
