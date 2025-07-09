using Discord;
using Discord.Interactions;
using SectomSharp.Attributes;
using SectomSharp.Data.Enums;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Moderation;

public sealed partial class ModerationModule
{
    [SlashCmd("Set the nickname of a user in the server")]
    [DefaultMemberPermissions(GuildPermission.ManageNicknames)]
    [RequireBotPermission(GuildPermission.ManageNicknames)]
    public async Task Nick([DoHierarchyCheck] IGuildUser user, string nickname, [ReasonMaxLength] string? reason = null)
    {
        if (user.Nickname == nickname)
        {
            await RespondAsync("Current nickname is already set to given nickname.", ephemeral: true);
            return;
        }

        await DeferAsync();
        await user.ModifyAsync(properties => properties.Nickname = nickname, DiscordUtils.GetAuditReasonRequestOptions(Context, reason));
        await CaseUtils.LogAsync(DbContextFactory, Context, BotLogType.Nick, OperationType.Create, user.Id, reason: reason);
    }
}
