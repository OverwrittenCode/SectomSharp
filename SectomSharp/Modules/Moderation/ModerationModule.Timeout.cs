using Discord;
using Discord.Interactions;
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
        [Summary(
            description: "Allowed formats: 4d3h2m1s, 4d3h, 3h2m1s, 3h1s, 2m, 20s (d=days, h=hours, m=minutes, s=seconds)"
        )]
            TimeSpan duration,
        [MaxLength(CaseService.MaxReasonLength)] string? reason = null
    )
    {
        if (duration > Constants.MaxTimeout)
        {
            await Context.Interaction.RespondAsync(
                $"Duration cannot exceed {Constants.MaxTimeout.Days} days.",
                ephemeral: true
            );
            return;
        }

        if (duration < Constants.MinTimeout)
        {
            await Context.Interaction.RespondAsync(
                $"Duration must be at least {Constants.MinTimeout.Seconds} seconds.",
                ephemeral: true
            );
            return;
        }

        var operationType = user.TimedOutUntil is null
            ? OperationType.Create
            : OperationType.Update;

        await Context.Interaction.DeferAsync();
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
            await Context.Interaction.RespondAsync(
                "This user is not timed out on the server.",
                ephemeral: true
            );
            return;
        }

        await Context.Interaction.DeferAsync();
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
