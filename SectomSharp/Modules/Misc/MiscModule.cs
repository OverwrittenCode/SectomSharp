using Discord;
using Discord.Interactions;
using SectomSharp.Attributes;

namespace SectomSharp.Modules.Misc;

[Category("Misc", "🛠️")]
[RateLimit]
[CommandContextType(InteractionContextType.Guild)]
public sealed partial class MiscModule : BaseModule
{
    private readonly InteractionService _commands;

    public MiscModule(InteractionService commands)
    {
        _commands = commands;
    }
}
