using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;

namespace SectomSharp.Modules.Misc;

[Category(nameof(Misc), "🛠️")]
[RateLimit]
[CommandContextType(InteractionContextType.Guild)]
public sealed partial class MiscModule : BaseModule<MiscModule>
{
    private readonly InteractionService _commands;

    public MiscModule(ILogger<MiscModule> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory, InteractionService commands) : base(logger, dbContextFactory)
        => _commands = commands;
}
