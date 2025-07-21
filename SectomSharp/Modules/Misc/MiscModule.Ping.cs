using SectomSharp.Attributes;

namespace SectomSharp.Modules.Misc;

public sealed partial class MiscModule
{
    [SlashCmd("Get the latency of the bot in milliseconds")]
    public Task Ping() => RespondAsync($"🏓 Pong! {Context.Client.Latency}ms", ephemeral: true);
}
