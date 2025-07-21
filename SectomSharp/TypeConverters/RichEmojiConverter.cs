using Discord;
using Discord.Interactions;

namespace SectomSharp.TypeConverters;

internal sealed class RichEmojiConverter : TypeConverter<IEmote>
{
    private static readonly TypeConverterResult InvalidDiscordGuildEmoji = TypeConverterResult.FromError(InteractionCommandError.ConvertFailed, "Invalid discord guild emoji");

    /// <inheritdoc />
    public override ApplicationCommandOptionType GetDiscordType() => ApplicationCommandOptionType.String;

    /// <inheritdoc />
    public override Task<TypeConverterResult> ReadAsync(IInteractionContext context, IApplicationCommandInteractionDataOption option, IServiceProvider services)
        => option.Value is not string s
            ? Task.FromResult(InvalidDiscordGuildEmoji)
            : Emoji.TryParse(s, out Emoji? emoji)
                ? Task.FromResult(TypeConverterResult.FromSuccess(emoji))
                : Task.FromResult(
                    Emote.TryParse(s, out Emote? emote) && context.Guild.Emotes.Any(e => e.Id == emote.Id) ? TypeConverterResult.FromSuccess(emote) : InvalidDiscordGuildEmoji
                );
}
