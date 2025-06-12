using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;

namespace SectomSharp.Modules.Misc;

[Category(nameof(Misc), "🛠️")]
[RateLimit]
[CommandContextType(InteractionContextType.Guild)]
public sealed partial class MiscModule : BaseModule<MiscModule>
{
    private readonly InteractionService _commands;

    public MiscModule(ILogger<MiscModule> logger, InteractionService commands) : base(logger) => _commands = commands;
}
