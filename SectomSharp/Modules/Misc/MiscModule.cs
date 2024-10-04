using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using SectomSharp.Attributes;

namespace SectomSharp.Modules.Misc;

[Category("Misc", "🛠️")]
[RateLimit]
[CommandContextType(InteractionContextType.Guild)]
public sealed partial class MiscModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly InteractionService _commands;

    public MiscModule(InteractionService commands, IServiceScopeFactory scopeFactory)
    {
        _commands = commands;
    }
}
