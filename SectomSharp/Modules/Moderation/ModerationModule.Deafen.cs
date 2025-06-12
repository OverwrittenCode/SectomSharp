using Discord;
using Discord.Interactions;
using SectomSharp.Attributes;
using SectomSharp.Data.Enums;
using SectomSharp.Services;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Moderation;

public sealed partial class ModerationModule
{
    [SlashCmd("Deafen a user in their current voice channel")]
    [DefaultMemberPermissions(GuildPermission.DeafenMembers)]
    [RequireBotPermission(GuildPermission.DeafenMembers)]
    public async Task Deafen([DoHierarchyCheck] IGuildUser user, [ReasonMaxLength] string? reason = null)
    {
        if (user.IsDeafened)
        {
            await RespondOrFollowUpAsync("User is already deafened.", ephemeral: true);
            return;
        }

        await DeferAsync();
        await user.ModifyAsync(properties => properties.Deaf = true, DiscordUtils.GetAuditReasonRequestOptions(Context, reason));
        await CaseUtils.LogAsync(Context, BotLogType.Deafen, OperationType.Create, user.Id, reason: reason);
    }
}
