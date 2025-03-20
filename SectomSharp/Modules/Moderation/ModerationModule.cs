using Discord;
using Discord.Interactions;
using SectomSharp.Attributes;

namespace SectomSharp.Modules.Moderation;

[Category(nameof(Moderation), "🛡️")]
[RateLimit]
[CommandContextType(InteractionContextType.Guild)]
public sealed partial class ModerationModule : BaseModule;
