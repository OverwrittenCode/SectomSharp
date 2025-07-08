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
    private readonly ILoggerFactory _loggerFactory;

    public MiscModule(ILogger<MiscModule> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory, ILoggerFactory loggerFactory) : base(logger, dbContextFactory)
        => _loggerFactory = loggerFactory;
}
