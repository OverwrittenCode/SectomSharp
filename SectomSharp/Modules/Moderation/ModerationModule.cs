using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;

namespace SectomSharp.Modules.Moderation;

[Category(nameof(Moderation), "🛡️")]
[RateLimit]
[CommandContextType(InteractionContextType.Guild)]
public sealed partial class ModerationModule : BaseModule<ModerationModule>
{
    /// <inheritdoc />
    public ModerationModule(ILogger<ModerationModule> logger) : base(logger) { }
}
