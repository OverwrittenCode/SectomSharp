using Discord.Interactions;

namespace SectomSharp.Modules.Misc;

public sealed partial class MiscModule
{
    [SlashCommand("ping", "Get the latency of the bot in milliseconds")]
    public async Task Ping() => await RespondOrFollowUpAsync($"🏓 Pong! {Context.Client.Latency}ms", ephemeral: true);
}
