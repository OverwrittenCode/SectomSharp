using Discord;
using Discord.Interactions;
using SectomSharp.Attributes;
using SectomSharp.Data.Enums;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Moderation;

public sealed partial class ModerationModule
{
    [SlashCmd("Mute a user in their current voice channel")]
    [DefaultMemberPermissions(GuildPermission.MuteMembers)]
    [RequireBotPermission(GuildPermission.MuteMembers)]
    public async Task Mute([DoHierarchyCheck] IGuildUser user, [ReasonMaxLength] string? reason = null)
    {
        if (user.IsMuted)
        {
            await RespondOrFollowUpAsync("User is already muted.", ephemeral: true);
            return;
        }

        await DeferAsync();
        await user.ModifyAsync(properties => properties.Mute = true, DiscordUtils.GetAuditReasonRequestOptions(Context, reason));
        await CaseUtils.LogAsync(Context, BotLogType.Mute, OperationType.Create, user.Id, reason: reason);
    }
}
