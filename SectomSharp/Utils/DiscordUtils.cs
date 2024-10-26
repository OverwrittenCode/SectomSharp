using Discord;
using Discord.Interactions;
using SectomSharp.Extensions;

namespace SectomSharp.Utils;

/// <summary>
///     Extra utility methods for formatting.
/// </summary>
/// <remarks>
///     See <see cref="Format" /> for more details.
/// </remarks>
internal static class DiscordUtils
{
    /// <summary>
    ///     Gets a hyperlinked URL for a discord user's profile.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <returns>
    ///     The hyperlinked URL for the discord user's profile.
    /// </returns>
    public static string GetHyperlinkedUserProfile(ulong userId) =>
        Format.Url(userId.ToString(), $"https://discordapp.com/users/{userId}");

    /// <summary>
    ///     Gets a URL that jumps to a message.
    /// </summary>
    /// <param name="guildId">The guild id.</param>
    /// <param name="channelId">The channel id.</param>
    /// <param name="messageId">The message id.</param>
    /// <returns>
    ///     The URL for the message.
    /// </returns>
    public static string GetMessageUrl(ulong guildId, ulong channelId, ulong messageId) =>
        $"https://discord.com/channels/{guildId}/{channelId}/{messageId}";

    /// <summary>
    ///     Creates a new instance of <see cref="RequestOptions" />
    ///     with a set audit log reason.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="reason">The given reason.</param>
    /// <param name="extra">A list of key-value pairs to include.</param>
    /// <returns>A new instance of <see cref="RequestOptions" />.</returns>
    public static RequestOptions GetAuditReasonRequestOptions(
        SocketInteractionContext context,
        string? reason,
        List<KeyValuePair<string, object>>? extra = null
    )
    {
        const int MaxAuditReasonLength = 512;

        extra ??= [];

        extra.InsertRange(
            0,
            [
                new("Perpetrator", $"{context.User.Username} ({context.User.Id})"),
                new("Channel", $"{context.Channel.Name} ({context.Channel.Id})"),
                new("Reason", reason ?? "No reason provided."),
            ]
        );

        var auditReason = String
            .Join(" | ", extra.Select(kvp => $"[{kvp.Key}]: {kvp.Value}"))
            .Truncate(MaxAuditReasonLength);

        return new() { AuditLogReason = auditReason };
    }
}
