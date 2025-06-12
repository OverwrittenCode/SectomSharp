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
    ///     Creates a new instance of <see cref="RequestOptions" />
    ///     with a set audit log reason.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="reason">The given reason.</param>
    /// <param name="extra">A list of key-value pairs to include.</param>
    /// <returns>A new instance of <see cref="RequestOptions" />.</returns>
    public static RequestOptions GetAuditReasonRequestOptions(SocketInteractionContext context, string? reason, List<KeyValuePair<string, string>>? extra = null)
    {
        const int maxAuditReasonLength = 512;

        List<KeyValuePair<string, string>> keyValuePairs =
        [
            new("Perpetrator", $"{context.User.Username} ({context.User.Id})"),
            new("Channel", $"{context.Channel.Name} ({context.Channel.Id})"),
            new("Reason", reason ?? "No reason provided.")
        ];

        if (extra is null)
        {
            extra = keyValuePairs;
        }
        else
        {
            extra.InsertRange(0, keyValuePairs);
        }

        return new RequestOptions
        {
            AuditLogReason = String.Join(" | ", extra.Select(kvp => $"[{kvp.Key}]: {kvp.Value}")).Truncate(maxAuditReasonLength)
        };
    }
}
