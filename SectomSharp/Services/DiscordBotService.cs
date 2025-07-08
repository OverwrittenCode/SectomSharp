using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SectomSharp.Events;
using SectomSharp.Extensions;
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
        switch (message.Severity)
        {
            case LogSeverity.Critical:
                _logger.DiscordNetCritical(message.Source, message.Message, message.Exception);
                break;
            case LogSeverity.Error:
                _logger.DiscordNetError(message.Source, message.Message, message.Exception);
                break;
            case LogSeverity.Warning:
                _logger.DiscordNetWarning(message.Source, message.Message, message.Exception);
                break;
            case LogSeverity.Info:
                _logger.DiscordNetInformation(message.Source, message.Message, message.Exception);
                break;
            case LogSeverity.Verbose:
                _logger.DiscordNetVerbose(message.Source, message.Message, message.Exception);
                break;
            case LogSeverity.Debug:
                _logger.DiscordNetDebug(message.Source, message.Message, message.Exception);
                break;
            default:
                _logger.DiscordNetInformation(message.Source, message.Message, message.Exception);
                break;
        }

        return Task.CompletedTask;
    }

    private async Task HandleClientReady()
    {
        await _client.SetGameAsync("Dev Mode", type: ActivityType.Watching);
        await _client.SetStatusAsync(UserStatus.Online);
        await _interactionService.RegisterCommandsToGuildAsync(Storage.ServerId);

        _client.Ready -= HandleClientReady;
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

        _client.MessageReceived += _discord.HandleMessageReceivedAsync;
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
