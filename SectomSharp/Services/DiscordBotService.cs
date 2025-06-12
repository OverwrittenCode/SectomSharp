using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SectomSharp.Events;
using SectomSharp.Utils;

namespace SectomSharp.Services;

public sealed class DiscordBotService : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly IConfiguration _config;
    private readonly ILogger<DiscordBotService> _logger;

    public DiscordBotService(DiscordSocketClient client, InteractionService interactionService, IConfiguration config, ILogger<DiscordBotService> logger)
    {
        _client = client;
        _interactionService = interactionService;
        _config = config;
        _logger = logger;
    }

    private Task LogAsync(LogMessage message)
    {
        LogLevel level = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Trace,
            LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.Information
        };

        _logger.Log(level, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
        return Task.CompletedTask;
    }

    private async Task HandleClientReady()
    {
        await _client.SetGameAsync("Dev Mode", type: ActivityType.Watching);
        await _client.SetStatusAsync(UserStatus.Online);
        await _interactionService.RegisterCommandsToGuildAsync(Storage.ServerId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string token = _config["Discord:BotToken"] ?? throw new InvalidOperationException("Missing bot token");

        _client.Ready += HandleClientReady;
        _client.Log += LogAsync;
        _interactionService.Log += LogAsync;

        _client.GuildUpdated += DiscordEvent.HandleGuildUpdatedAsync;

        _client.GuildMemberUpdated += DiscordEvent.HandleGuildMemberUpdatedAsync;

        _client.MessageDeleted += DiscordEvent.HandleMessageDeletedAsync;
        _client.MessageUpdated += DiscordEvent.HandleMessageUpdatedAsync;

        _client.GuildStickerCreated += DiscordEvent.HandleGuildStickerCreatedAsync;
        _client.GuildStickerDeleted += DiscordEvent.HandleGuildStickerDeletedAsync;
        _client.GuildStickerUpdated += DiscordEvent.HandleGuildStickerUpdatedAsync;

        _client.ChannelCreated += DiscordEvent.HandleChannelCreatedAsync;
        _client.ChannelDestroyed += DiscordEvent.HandleChannelDestroyedAsync;
        _client.ChannelUpdated += DiscordEvent.HandleChannelUpdatedAsync;

        _client.ThreadCreated += DiscordEvent.HandleThreadCreatedAsync;
        _client.ThreadDeleted += DiscordEvent.HandleThreadDeleteAsync;
        _client.ThreadUpdated += DiscordEvent.HandleThreadUpdatedAsync;

        _client.RoleCreated += DiscordEvent.HandleRoleCreatedAsync;
        _client.RoleDeleted += DiscordEvent.HandleRoleDeletedAsync;
        _client.RoleUpdated += DiscordEvent.HandleRoleUpdateAsync;

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        if (ExecuteTask is null)
        {
            return Task.CompletedTask;
        }

        base.StopAsync(cancellationToken);
        return _client.StopAsync();
    }
}
