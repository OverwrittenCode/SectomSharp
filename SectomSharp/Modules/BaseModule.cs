using Discord;
using Discord.Interactions;
using SectomSharp.Extensions;

namespace SectomSharp.Modules;

/// <inheritdoc/>
public class BaseModule : InteractionModuleBase<SocketInteractionContext>
{
    /// <inheritdoc cref="InteractionExtensions.RespondOrFollowupAsync"/>
    public async Task RespondOrFollowUpAsync(
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
}
