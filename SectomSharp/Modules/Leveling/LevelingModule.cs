using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;

namespace SectomSharp.Modules.Leveling;

[Category(nameof(Leveling), "🏆")]
[RateLimit]
[CommandContextType(InteractionContextType.Guild)]
public sealed partial class LevelingModule : BaseModule<LevelingModule>
{
    /// <inheritdoc />
    public LevelingModule(ILogger<BaseModule<LevelingModule>> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(logger, dbContextFactory) { }
}
