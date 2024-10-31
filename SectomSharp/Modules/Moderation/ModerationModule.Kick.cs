using Discord;
using Discord.Interactions;
using SectomSharp.Data.Enums;
using SectomSharp.Services;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Moderation;

public sealed partial class ModerationModule
{
    [SlashCommand("kick", "Kicks a user from the server")]
    [DefaultMemberPermissions(GuildPermission.KickMembers)]
    [RequireBotPermission(GuildPermission.KickMembers)]
    public async Task Kick([DoHierarchyCheck] IGuildUser user, [MaxLength(CaseService.MaxReasonLength)] string? reason = null)
    {
        await DeferAsync();
        await user.KickAsync(options: DiscordUtils.GetAuditReasonRequestOptions(Context, reason));
        await CaseService.LogAsync(Context, BotLogType.Kick, OperationType.Create, user.Id, reason: reason);
    }
}
