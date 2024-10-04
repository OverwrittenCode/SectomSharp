using Discord.Interactions;

namespace SectomSharp.Modules.Misc;

public sealed partial class MiscModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("ping", "Get the latency of the bot in milliseconds")]
    public async Task Ping() =>
        await RespondAsync($"🏓 Pong! {Context.Client.Latency}ms", ephemeral: true);
}
