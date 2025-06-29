using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;

namespace SectomSharp.Modules.Games;

[Category(nameof(Games), "🎮")]
[RateLimit]
[CommandContextType(InteractionContextType.Guild)]
public sealed partial class GameModule : BaseModule<GameModule>
{
    /// <inheritdoc />
    public GameModule(ILogger<GameModule> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(logger, dbContextFactory) { }
}
