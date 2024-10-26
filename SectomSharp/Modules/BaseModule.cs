using Discord;
using Discord.Interactions;
using SectomSharp.Extensions;

namespace SectomSharp.Modules;

/// <inheritdoc />
public class BaseModule : InteractionModuleBase<SocketInteractionContext>
{
    /// <inheritdoc cref="DiscordExtensions.RespondOrFollowupAsync" />
    protected async Task RespondOrFollowUpAsync(
        string? text = null,
        Embed[]? embeds = null,
        bool ephemeral = false,
        AllowedMentions? allowedMentions = null,
        MessageComponent? components = null,
        RequestOptions? options = null,
        PollProperties? poll = null
    ) =>
        await Context.Interaction.RespondOrFollowupAsync(
            text,
            embeds,
            ephemeral,
            allowedMentions,
            components,
            options,
            poll
        );

    private protected const string TimespanDescription =
        "Allowed formats: 4d3h2m1s, 4d3h, 3h2m1s, 3h1s, 2m, 20s (d=days, h=hours, m=minutes, s=seconds)";
}
