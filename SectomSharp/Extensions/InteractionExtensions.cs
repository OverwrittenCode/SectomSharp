using Discord;

namespace SectomSharp.Extensions;

internal static class InteractionExtensions
{
    /// <summary>
    ///     Asynchronously responds or follows up an module
    ///     based on <see cref="IDiscordInteraction.HasResponded"/>.
    /// </summary>
    /// <returns>
    ///     A task representing the asynchronous operation of
    ///     responding or following up the interaction.
    /// </returns>
    /// <inheritdoc cref="IDiscordInteraction.RespondAsync"/>
    public static async Task RespondOrFollowupAsync(
        this IDiscordInteraction interaction,
        string? text = null,
        Embed[]? embeds = null,
        bool ephemeral = false,
        AllowedMentions? allowedMentions = null,
        MessageComponent? components = null,
        RequestOptions? options = null,
        PollProperties? poll = null
    )
    {
        if (interaction.HasResponded)
        {
            await interaction.FollowupAsync(
                text,
                embeds,
                isTTS: false,
                ephemeral,
                allowedMentions,
                components,
                embed: null,
                options,
                poll
            );
        }
        else
        {
            await interaction.RespondAsync(
                text,
                embeds,
                isTTS: false,
                ephemeral,
                allowedMentions,
                components,
                embed: null,
                options,
                poll
            );
        }
    }
}
