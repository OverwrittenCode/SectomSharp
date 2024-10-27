using Discord;
using Discord.Interactions;
using SectomSharp.Data.Enums;
using SectomSharp.Services;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Moderation;

public partial class ModerationModule
{
    [SlashCommand("mute", "Mute a user in their current voice channel")]
    [DefaultMemberPermissions(GuildPermission.MuteMembers)]
    [RequireBotPermission(GuildPermission.MuteMembers)]
    public async Task Mute(
        [DoHierarchyCheck] IGuildUser user,
        [MaxLength(CaseService.MaxReasonLength)] string? reason = null
    )
    {
        if (user.IsMuted)
        {
            await RespondOrFollowUpAsync("User is already muted.", ephemeral: true);
            return;
        }

        await DeferAsync();
        await user.ModifyAsync(
            properties => properties.Mute = true,
            DiscordUtils.GetAuditReasonRequestOptions(Context, reason)
        );
        await CaseService.LogAsync(
            Context,
            BotLogType.Mute,
            OperationType.Create,
            targetId: user.Id,
            reason: reason
        );
    }

    [SlashCommand("nick", "Set the nickname of a user in the server")]
    [DefaultMemberPermissions(GuildPermission.ManageNicknames)]
    [RequireBotPermission(GuildPermission.ManageNicknames)]
    public async Task Nick(
        [DoHierarchyCheck] IGuildUser user,
        string nickname,
        [MaxLength(CaseService.MaxReasonLength)] string? reason = null
    )
    {
        if (user.Nickname == nickname)
        {
            await RespondOrFollowUpAsync(
                "Current nickname is already set to given nickname.",
                ephemeral: true
            );
            return;
        }

        await DeferAsync();
        await user.ModifyAsync(
            properties => properties.Nickname = nickname,
            DiscordUtils.GetAuditReasonRequestOptions(Context, reason)
        );
        await CaseService.LogAsync(
            Context,
            BotLogType.Nick,
            OperationType.Create,
            targetId: user.Id,
            reason: reason
        );
    }

    [SlashCommand("mod-note", "Add a moderation note to a user in the server")]
    public async Task ModNote(
        [DoHierarchyCheck] IGuildUser user,
        [MaxLength(CaseService.MaxReasonLength)] string note
    )
    {
        await DeferAsync();
        await CaseService.LogAsync(
            Context,
            BotLogType.ModNote,
            OperationType.Create,
            targetId: user.Id,
            reason: note
        );
    }
}
