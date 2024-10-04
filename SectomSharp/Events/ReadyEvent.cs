using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using SectomSharp.Utils;

namespace SectomSharp.Events;

public sealed class ReadyEvent
{
    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _client;

    public ReadyEvent(IServiceProvider services, DiscordSocketClient client)
    {
        _services = services;
        _client = client;
    }

    public async Task OnReady()
    {
        await _client.SetGameAsync("Dev Mode", type: ActivityType.Watching);
        await _client.SetStatusAsync(UserStatus.Online);

        var interactions = new InteractionService(
            _client,
            new InteractionServiceConfig()
            {
                LogLevel = LogSeverity.Info,
                DefaultRunMode = RunMode.Async,
            }
        );

        await interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        await interactions.RegisterCommandsToGuildAsync(Constants.ServerID);
    }
}
