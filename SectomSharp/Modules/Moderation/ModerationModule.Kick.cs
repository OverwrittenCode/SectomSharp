using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using SectomSharp.Attributes;
using SectomSharp.Data.Enums;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Moderation;

public sealed partial class ModerationModule
{
    [SlashCmd("Kicks a user from the server")]
    [DefaultMemberPermissions(GuildPermission.KickMembers)]
    [RequireBotPermission(GuildPermission.KickMembers)]
    public async Task Kick([DoHierarchyCheck] SocketGuildUser user, [ReasonMaxLength] string? reason = null)
    {
        await DeferAsync();
        await user.KickAsync(options: DiscordUtils.GetAuditReasonRequestOptions(Context, reason));
        await CaseUtils.LogAsync(DbContextFactory, Context, BotLogType.Kick, OperationType.Create, user.Id, reason: reason);
    }
}
