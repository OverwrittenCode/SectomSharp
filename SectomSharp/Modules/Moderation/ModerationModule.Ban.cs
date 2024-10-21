using Discord;
using Discord.Interactions;
using SectomSharp.Data.Enums;
using SectomSharp.Services;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Moderation;

public partial class ModerationModule
{
    private static uint GetPruneSeconds(int pruneDays) => (uint)pruneDays * 86_400;

    [SlashCommand("ban", "Ban a user from the server")]
    [DefaultMemberPermissions(GuildPermission.BanMembers)]
    [RequireBotPermission(GuildPermission.BanMembers)]
    public async Task Ban(
        [DoHierarchyCheck] IUser user,
        [MinValue(0)] [MaxValue(7)] int pruneDays,
        [MaxLength(CaseService.MaxReasonLength)] string? reason = null
    )
    {
        if (await Context.Guild.GetBanAsync(user) is not null)
        {
            await Context.Interaction.RespondAsync(
                "This user is already banned on the server.",
                ephemeral: true
            );
            return;
        }

        await Context.Interaction.DeferAsync();

        await Context.Guild.BanUserAsync(
            user,
            GetPruneSeconds(pruneDays),
            DiscordUtils.GetAuditReasonRequestOptions(Context, reason)
        );

        await CaseService.LogAsync(
            Context,
            BotLogType.Ban,
            OperationType.Create,
            targetId: user.Id,
            reason: reason
        );
    }

    [SlashCommand(
        "softban",
        "Ban a user to prune their messages and then immediately unban them from the server"
    )]
    [DefaultMemberPermissions(GuildPermission.BanMembers)]
    [RequireBotPermission(GuildPermission.BanMembers)]
    public async Task SoftBan(
        [DoHierarchyCheck] IUser user,
        [MinValue(0)] [MaxValue(7)] int pruneDays,
        [MaxLength(CaseService.MaxReasonLength)] string? reason = null
    )
    {
        if (await Context.Guild.GetBanAsync(user) is not null)
        {
            await Context.Interaction.RespondAsync(
                "This user is already banned on the server.",
                ephemeral: true
            );
            return;
        }

        var requestOptions = DiscordUtils.GetAuditReasonRequestOptions(
            Context,
            reason,
            [new("Operation", BotLogType.Softban)]
        );

        await Context.Interaction.DeferAsync();
        await Context.Guild.BanUserAsync(user, GetPruneSeconds(pruneDays), requestOptions);
        await Context.Guild.RemoveBanAsync(user, requestOptions);
        await CaseService.LogAsync(
            Context,
            BotLogType.Softban,
            OperationType.Create,
            targetId: user.Id,
            reason: reason
        );
    }

    [SlashCommand("unban", "Unban a user from the server")]
    [DefaultMemberPermissions(GuildPermission.BanMembers)]
    [RequireBotPermission(GuildPermission.BanMembers)]
    public async Task Unban(
        [DoHierarchyCheck] IUser user,
        [MaxLength(CaseService.MaxReasonLength)] string? reason = null
    )
    {
        if (await Context.Guild.GetBanAsync(user) is null)
        {
            await Context.Interaction.RespondAsync(
                "This user is not banned from the server.",
                ephemeral: true
            );
            return;
        }

        await Context.Interaction.DeferAsync();

        await Context.Guild.RemoveBanAsync(
            user,
            DiscordUtils.GetAuditReasonRequestOptions(Context, reason)
        );

        await CaseService.LogAsync(
            Context,
            BotLogType.Ban,
            OperationType.Delete,
            targetId: user.Id,
            reason: reason
        );
    }
}
