using Discord;
using Discord.Interactions;
using SectomSharp.Attributes;
using SectomSharp.Data.Enums;
using SectomSharp.Services;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Moderation;

public partial class ModerationModule
{
    private static uint GetPruneSeconds(int pruneDays) => (uint)pruneDays * 86_400;

    [SlashCmd("Ban a user from the server")]
    [DefaultMemberPermissions(GuildPermission.BanMembers)]
    [RequireBotPermission(GuildPermission.BanMembers)]
    public async Task Ban([DoHierarchyCheck] IUser user, [MinValue(0)] [MaxValue(7)] int pruneDays, [ReasonMaxLength] string? reason = null)
    {
        if (await Context.Guild.GetBanAsync(user) is not null)
        {
            await RespondOrFollowUpAsync("This user is already banned on the server.", ephemeral: true);
            return;
        }

        await DeferAsync();
        await Context.Guild.BanUserAsync(user, GetPruneSeconds(pruneDays), DiscordUtils.GetAuditReasonRequestOptions(Context, reason));
        await CaseService.LogAsync(Context, BotLogType.Ban, OperationType.Create, user.Id, reason: reason);
    }

    [SlashCmd("Ban a user to prune their messages and then immediately unban them from the server")]
    [DefaultMemberPermissions(GuildPermission.BanMembers)]
    [RequireBotPermission(GuildPermission.BanMembers)]
    public async Task Softban([DoHierarchyCheck] IUser user, [MinValue(0)] [MaxValue(7)] int pruneDays, [ReasonMaxLength] string? reason = null)
    {
        if (await Context.Guild.GetBanAsync(user) is not null)
        {
            await RespondOrFollowUpAsync("This user is already banned on the server.", ephemeral: true);
            return;
        }

        RequestOptions requestOptions = DiscordUtils.GetAuditReasonRequestOptions(Context, reason, [new KeyValuePair<string, object>("Operation", BotLogType.Softban)]);

        await DeferAsync();
        await Context.Guild.BanUserAsync(user, GetPruneSeconds(pruneDays), requestOptions);
        await Context.Guild.RemoveBanAsync(user, requestOptions);
        await CaseService.LogAsync(Context, BotLogType.Softban, OperationType.Create, user.Id, reason: reason);
    }

    [SlashCmd("Unban a user from the server")]
    [DefaultMemberPermissions(GuildPermission.BanMembers)]
    [RequireBotPermission(GuildPermission.BanMembers)]
    public async Task Unban([DoHierarchyCheck] IUser user, [ReasonMaxLength] string? reason = null)
    {
        if (await Context.Guild.GetBanAsync(user) is null)
        {
            await RespondOrFollowUpAsync("This user is not banned from the server.", ephemeral: true);
            return;
        }

        await DeferAsync();
        await Context.Guild.RemoveBanAsync(user, DiscordUtils.GetAuditReasonRequestOptions(Context, reason));
        await CaseService.LogAsync(Context, BotLogType.Ban, OperationType.Delete, user.Id, reason: reason);
    }
}
