using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;

namespace SectomSharp.Modules.Games;

[Category(nameof(Games), "🎮")]
[RateLimit]
[CommandContextType(InteractionContextType.Guild)]
public sealed partial class GameModule : BaseModule<GameModule>
{
    /// <inheritdoc />
    public GameModule(ILogger<GameModule> logger) : base(logger) { }
}
