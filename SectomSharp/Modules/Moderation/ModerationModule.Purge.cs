using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using SectomSharp.Data.Enums;
using SectomSharp.Services;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Moderation;

public sealed partial class ModerationModule
{
    [SlashCommand("purge", "Bulk delete messages in the current channel")]
    [DefaultMemberPermissions(GuildPermission.ManageMessages)]
    [RequireBotPermission(ChannelPermission.ManageMessages)]
    public async Task Purge(
        [MinValue(1)] [MaxValue(DiscordConfig.MaxMessagesPerBatch)] int amount = 50,
        [MaxLength(CaseService.MaxReasonLength)] string? reason = null
    )
    {
        await DeferAsync();
        IUserMessage originalMessage = await GetOriginalResponseAsync();

        DateTime earliestAllowedPurgeDateTime = DateTime.UtcNow.AddDays(-14);

        var channel = (SocketTextChannel)Context.Channel;

        List<IMessage> messages = (await channel.GetMessagesAsync(amount + 1).FlattenAsync())
            .Where(message =>
                message.Id != originalMessage.Id
                && message.CreatedAt >= earliestAllowedPurgeDateTime
            )
            .ToList();

        await channel.DeleteMessagesAsync(
            messages,
            DiscordUtils.GetAuditReasonRequestOptions(Context, reason)
        );
        await CaseService.LogAsync(
            Context,
            BotLogType.Purge,
            OperationType.Create,
            channelId: channel.Id,
            reason: reason
        );
        await RespondOrFollowUpAsync($"Deleted {messages.Count}/{amount} messages.");
    }
}
