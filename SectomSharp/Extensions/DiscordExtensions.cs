using Discord;

namespace SectomSharp.Extensions;

internal static class DiscordExtensions
{
    /// <summary>
    ///     Asynchronously responds or follows up a module based on <see cref="IDiscordInteraction.HasResponded" />.
    /// </summary>
    /// <returns>A task representing the asynchronous operation of responding or following up the interaction.</returns>
    /// <inheritdoc cref="IDiscordInteraction.RespondAsync" />
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
            await interaction.FollowupAsync(text, embeds, false, ephemeral, allowedMentions, components, null, options, poll);
        }
        else
        {
            await interaction.RespondAsync(text, embeds, false, ephemeral, allowedMentions, components, null, options, poll);
        }
    }

    /// <summary>
    ///     Converts a Discord <see cref="Color" /> to a hyperlinked
    ///     URL that displays its hex code.
    /// </summary>
    /// <returns>The URL.</returns>
    public static string ToHyperlinkedColourPicker(this Color colour)
    {
        const string prefix = "[#";
        const string middleFix = "](https://imagecolorpicker.com/color-code/";
        const string suffix = ")";
        const int hexLength = 6;

        int totalLength = prefix.Length + hexLength + middleFix.Length + hexLength + suffix.Length;

        return String.Create(
            totalLength,
            colour.RawValue,
            static (span, value) =>
            {
                Span<char> hex = stackalloc char[hexLength];
                value.TryFormat(hex, out _, "X6");

                prefix.CopyTo(span);
                int i = prefix.Length;
                hex.CopyTo(span[i..]);
                i += hexLength;
                middleFix.CopyTo(span[i..]);
                i += middleFix.Length;
                hex.CopyTo(span[i..]);
                i += 6;
                suffix.CopyTo(span[i..]);
            }
        );
    }
}
