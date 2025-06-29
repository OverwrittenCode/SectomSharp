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
    private readonly DiscordEvent _discord;
    private readonly InteractionService _interactionService;
    private readonly IConfiguration _config;
    private readonly ILogger<DiscordBotService> _logger;

    public DiscordBotService(DiscordSocketClient client, DiscordEvent discord, InteractionService interactionService, IConfiguration config, ILogger<DiscordBotService> logger)
    {
        _client = client;
        _discord = discord;
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

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string token = _config["Discord:BotToken"] ?? throw new InvalidOperationException("Missing bot token");

        _client.Ready += HandleClientReady;
        _client.Log += LogAsync;
        _interactionService.Log += LogAsync;

        _client.GuildUpdated += _discord.HandleGuildUpdatedAsync;

        _client.GuildMemberUpdated += _discord.HandleGuildMemberUpdatedAsync;

        // _client.MessageReceived += DiscordEvent.HandleMessageReceivedAsync;
        _client.MessageDeleted += _discord.HandleMessageDeletedAsync;
        _client.MessageUpdated += _discord.HandleMessageUpdatedAsync;

        _client.GuildStickerCreated += _discord.HandleGuildStickerCreatedAsync;
        _client.GuildStickerDeleted += _discord.HandleGuildStickerDeletedAsync;
        _client.GuildStickerUpdated += _discord.HandleGuildStickerUpdatedAsync;

        _client.ChannelCreated += _discord.HandleChannelCreatedAsync;
        _client.ChannelDestroyed += _discord.HandleChannelDestroyedAsync;
        _client.ChannelUpdated += _discord.HandleChannelUpdatedAsync;

        _client.ThreadCreated += _discord.HandleThreadCreatedAsync;
        _client.ThreadDeleted += _discord.HandleThreadDeleteAsync;
        _client.ThreadUpdated += _discord.HandleThreadUpdatedAsync;

        _client.RoleCreated += _discord.HandleRoleCreatedAsync;
        _client.RoleDeleted += _discord.HandleRoleDeletedAsync;
        _client.RoleUpdated += _discord.HandleRoleUpdateAsync;

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    /// <inheritdoc />
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
