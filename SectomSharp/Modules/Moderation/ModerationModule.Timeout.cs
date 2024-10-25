using Discord;
using Discord.Interactions;
using SectomSharp.Attributes;
using SectomSharp.Data.Enums;
using SectomSharp.Services;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Moderation;

public partial class ModerationModule
{
    [SlashCommand("timeout", "Timeout a user on the server")]
    [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
    [RequireBotPermission(GuildPermission.ModerateMembers)]
    public async Task Timeout(
        [DoHierarchyCheck] IGuildUser user,
        [Summary(description: TimespanDescription)] [TimeoutRange] TimeSpan duration,
        [MaxLength(CaseService.MaxReasonLength)] string? reason = null
    )
    {
        if (user.IsBot)
        {
            await RespondOrFollowUpAsync("Bot users cannot be timed out.", ephemeral: true);
            return;
        }

        OperationType operationType = user.TimedOutUntil is null
            ? OperationType.Create
            : OperationType.Update;

        await DeferAsync();
        await user.SetTimeOutAsync(
            duration,
            DiscordUtils.GetAuditReasonRequestOptions(Context, reason)
        );

        await CaseService.LogAsync(
            Context,
            BotLogType.Timeout,
            operationType,
            targetId: user.Id,
            expiresAt: user.TimedOutUntil?.UtcDateTime,
            reason: reason
        );
    }

    [SlashCommand("untimeout", "Remove a timeout from a user on the server")]
    [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
    [RequireBotPermission(GuildPermission.ModerateMembers)]
    public async Task Untimeout(
        [DoHierarchyCheck] IGuildUser user,
        [MaxLength(CaseService.MaxReasonLength)] string? reason = null
    )
    {
        if (user.TimedOutUntil is null)
        {
            await RespondOrFollowUpAsync(
                "This user is not timed out on the server.",
                ephemeral: true
            );
            return;
        }

        await DeferAsync();
        await user.RemoveTimeOutAsync(DiscordUtils.GetAuditReasonRequestOptions(Context, reason));

        await CaseService.LogAsync(
            Context,
            BotLogType.Timeout,
            OperationType.Delete,
            targetId: user.Id,
            reason: reason
        );
    }
}
