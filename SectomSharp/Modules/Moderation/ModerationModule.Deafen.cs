using Discord;
using Discord.Interactions;
using SectomSharp.Data.Enums;
using SectomSharp.Services;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Moderation;

public partial class ModerationModule
{
    [SlashCommand("deafen", "Deafen a user in their current voice channel")]
    [DefaultMemberPermissions(GuildPermission.DeafenMembers)]
    [RequireBotPermission(GuildPermission.DeafenMembers)]
    public async Task Deafen(
        [DoHierarchyCheck] IGuildUser user,
        [MaxLength(CaseService.MaxReasonLength)] string? reason = null
    )
    {
        if (user.IsDeafened)
        {
            await RespondOrFollowUpAsync("User is already deafened.", ephemeral: true);
            return;
        }

        await DeferAsync();
        await user.ModifyAsync(
            properties => properties.Deaf = true,
            DiscordUtils.GetAuditReasonRequestOptions(Context, reason)
        );
        await CaseService.LogAsync(
            Context,
            BotLogType.Deafen,
            OperationType.Create,
            targetId: user.Id,
            reason: reason
        );
    }
}
