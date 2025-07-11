using Discord;
using Discord.Interactions;

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
    ///     Creates a new instance of <see cref="RequestOptions" /> with a set audit log reason.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="reason">The given reason.</param>
    /// <returns>A new instance of <see cref="RequestOptions" />.</returns>
    public static RequestOptions GetAuditReasonRequestOptions(SocketInteractionContext context, string? reason)
        => new()
        {
            AuditLogReason =
                $"[Perpetrator]: {context.User.Username} ({context.User.Id}) | [Channel]: {context.Channel.Name} ({context.Channel.Id}) | [Reason]: {reason ?? "No reason provided."}"
        };

    /// <summary>
    ///     Creates a new instance of <see cref="RequestOptions" /> with a set audit log reason.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="reason">The given reason.</param>
    /// <param name="metadata">The extra metadata.</param>
    /// <returns>A new instance of <see cref="RequestOptions" />.</returns>
    public static RequestOptions GetAuditReasonRequestOptions(SocketInteractionContext context, string? reason, string metadata)
        => new()
        {
            AuditLogReason =
                $"[Perpetrator]: {context.User.Username} ({context.User.Id}) | [Channel]: {context.Channel.Name} ({context.Channel.Id}) | [Reason]: {reason ?? "No reason provided."} | {metadata}"
        };
}
