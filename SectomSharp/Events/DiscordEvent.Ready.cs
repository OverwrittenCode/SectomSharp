using System.Reflection;
using Discord;
using Discord.Interactions;
using SectomSharp.Utils;

namespace SectomSharp.Events;

public partial class DiscordEvent
{
    public async Task HandleClientReady()
    {
        await _client.SetGameAsync("Dev Mode", type: ActivityType.Watching);
        await _client.SetStatusAsync(UserStatus.Online);

        var interactions = new InteractionService(
            _client,
            new() { LogLevel = LogSeverity.Info, DefaultRunMode = RunMode.Async }
        );

        await interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        await interactions.RegisterCommandsToGuildAsync(Constants.ServerId);
    }
}
