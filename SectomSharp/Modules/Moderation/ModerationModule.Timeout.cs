using Discord;
using Discord.Interactions;
using SectomSharp.Attributes;
using SectomSharp.Data.Enums;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Moderation;

public sealed partial class ModerationModule
{
    [SlashCmd("Timeout a user on the server")]
    [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
    [RequireBotPermission(GuildPermission.ModerateMembers)]
    public async Task Timeout(
        [DoHierarchyCheck] IGuildUser user,
        [Summary(description: TimespanDescription)] [TimeoutRange] TimeSpan duration,
        [ReasonMaxLength] string? reason = null
    )
    {
        if (user.IsBot)
        {
            await RespondAsync("Bot users cannot be timed out.", ephemeral: true);
            return;
        }

        OperationType operationType = user.TimedOutUntil is null ? OperationType.Create : OperationType.Update;

        await DeferAsync();
        await user.SetTimeOutAsync(duration, DiscordUtils.GetAuditReasonRequestOptions(Context, reason));

        await CaseUtils.LogAsync(DbContextFactory, Context, BotLogType.Timeout, operationType, user.Id, expiresAt: user.TimedOutUntil?.UtcDateTime, reason: reason);
    }

    [SlashCmd("Remove a timeout from a user on the server")]
    [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
    [RequireBotPermission(GuildPermission.ModerateMembers)]
    public async Task Untimeout([DoHierarchyCheck] IGuildUser user, [ReasonMaxLength] string? reason = null)
    {
        if (user.TimedOutUntil is null)
        {
            await RespondAsync("This user is not timed out on the server.", ephemeral: true);
            return;
        }

        await DeferAsync();
        await user.RemoveTimeOutAsync(DiscordUtils.GetAuditReasonRequestOptions(Context, reason));

        await CaseUtils.LogAsync(DbContextFactory, Context, BotLogType.Timeout, OperationType.Delete, user.Id, reason: reason);
    }
}
