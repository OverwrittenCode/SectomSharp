using Discord;
using Discord.Interactions;
using JetBrains.Annotations;
using SectomSharp.Extensions;

namespace SectomSharp.Modules;

/// <inheritdoc />
[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.WithInheritors)]
public abstract class BaseModule : InteractionModuleBase<SocketInteractionContext>
{
    private protected const string TimespanDescription = "Allowed formats: 4d3h2m1s, 4d3h, 3h2m1s, 3h1s, 2m, 20s (d=days, h=hours, m=minutes, s=seconds)";

    /// <inheritdoc cref="DiscordExtensions.RespondOrFollowupAsync" />
    protected async Task RespondOrFollowUpAsync(
        string? text = null,
        Embed[]? embeds = null,
        bool ephemeral = false,
        AllowedMentions? allowedMentions = null,
        MessageComponent? components = null,
        RequestOptions? options = null,
        PollProperties? poll = null
    )
        => await Context.Interaction.RespondOrFollowupAsync(text, embeds, ephemeral, allowedMentions, components, options, poll);
}
