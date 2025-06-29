using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;

namespace SectomSharp.Modules.Moderation;

[Category(nameof(Moderation), "🛡️")]
[RateLimit]
[CommandContextType(InteractionContextType.Guild)]
public sealed partial class ModerationModule : BaseModule<ModerationModule>
{
    /// <inheritdoc />
    public ModerationModule(ILogger<ModerationModule> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(logger, dbContextFactory) { }
}
