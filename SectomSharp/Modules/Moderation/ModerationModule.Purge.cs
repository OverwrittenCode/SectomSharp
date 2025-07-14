using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using SectomSharp.Attributes;
using SectomSharp.Data.Enums;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Moderation;

public sealed partial class ModerationModule
{
    [SlashCmd("Bulk delete messages in the current channel")]
    [DefaultMemberPermissions(GuildPermission.ManageMessages)]
    [RequireBotPermission(ChannelPermission.ManageMessages)]
    public async Task Purge([MinValue(1)] [MaxValue(DiscordConfig.MaxMessagesPerBatch)] int amount = 50, [ReasonMaxLength] string? reason = null)
    {
        await DeferAsync();
        IUserMessage originalMessage = await GetOriginalResponseAsync();

        DateTimeOffset earliestAllowedPurgeDateTime = DateTimeOffset.UtcNow.AddDays(-14);
        var channel = (SocketTextChannel)Context.Channel;

        ulong[] messageIds = await channel.GetMessagesAsync(amount + 1)
                                          .SelectMany(page => page.Where(message => message.Id != originalMessage.Id && message.CreatedAt >= earliestAllowedPurgeDateTime)
                                                                  .Select(message => message.Id)
                                                                  .ToAsyncEnumerable()
                                           )
                                          .ToArrayAsync();
        if (messageIds.Length == 0)
        {
            await FollowupAsync("No messages newer than 2 weeks were found.");
            return;
        }

        await channel.DeleteMessagesAsync(messageIds, DiscordUtils.GetAuditReasonRequestOptions(Context, reason));
        await CaseUtils.LogAsync(DbContextFactory, Context, BotLogType.Purge, OperationType.Create, channelId: channel.Id, reason: reason);
    }
}
