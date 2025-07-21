using Discord;
using Discord.Interactions;

namespace SectomSharp.TypeConverters;

internal sealed class ColorConverter : TypeConverter<Color>
{
    /// <inheritdoc />
    public override ApplicationCommandOptionType GetDiscordType() => ApplicationCommandOptionType.String;

    /// <inheritdoc />
    public override Task<TypeConverterResult> ReadAsync(IInteractionContext context, IApplicationCommandInteractionDataOption option, IServiceProvider services)
        => option.Value is string s && Color.TryParse(s, out Color color)
            ? Task.FromResult(TypeConverterResult.FromSuccess(color))
            : Task.FromResult(TypeConverterResult.FromError(InteractionCommandError.ConvertFailed, "Invalid hex code"));
}
