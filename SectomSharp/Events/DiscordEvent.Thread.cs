using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using SectomSharp.Data.Enums;

namespace SectomSharp.Events;

public sealed partial class DiscordEvent
{
    private async Task HandleThreadAlteredAsync(SocketThreadChannel thread, OperationType operationType)
    {
        using DiscordWebhookClient? webhookClient = await GetDiscordWebhookClientAsync(thread.Guild, AuditLogType.Thread);
        if (webhookClient is null)
        {
            return;
        }

        List<AuditLogEntry> entries =
        [
            new("Id", thread.Id),
            new("Name", thread.Name),
            new("Type", thread.Type),
            new("Parent", MentionUtils.MentionChannel(thread.ParentChannel.Id)),
            new("Topic", thread.Topic)
        ];

        await LogAsync(thread.Guild, webhookClient, AuditLogType.Thread, operationType, entries, thread.Id.ToString(), thread.Name);
    }

    public async Task HandleThreadCreatedAsync(SocketThreadChannel thread) => await HandleThreadAlteredAsync(thread, OperationType.Create);

    public async Task HandleThreadDeleteAsync(Cacheable<SocketThreadChannel, ulong> partialThread)
        => await HandleThreadAlteredAsync(await partialThread.GetOrDownloadAsync(), OperationType.Delete);

    public async Task HandleThreadUpdatedAsync(Cacheable<SocketThreadChannel, ulong> oldPartialThread, SocketThreadChannel newThread)
    {
        using DiscordWebhookClient? webhookClient = await GetDiscordWebhookClientAsync(newThread.Guild, AuditLogType.Thread);
        if (webhookClient is null)
        {
            return;
        }

        SocketThreadChannel oldThread = await oldPartialThread.GetOrDownloadAsync();

        List<AuditLogEntry> entries =
        [
            new("Name", GetChangeEntry(oldThread.Name, newThread.Name), oldThread.Name != newThread.Name),
            new("Type", $"Set to {newThread.Type}", oldThread.Type != newThread.Type),
            new(
                "Parent",
                GetChangeEntry(MentionUtils.MentionChannel(oldThread.ParentChannel.Id), MentionUtils.MentionChannel(newThread.ParentChannel.Id)),
                oldThread.ParentChannel.Id != newThread.ParentChannel.Id
            ),
            new("Topic", GetChangeEntry(oldThread.Topic, newThread.Topic), oldThread.Topic != newThread.Topic)
        ];

        await LogAsync(newThread.Guild, webhookClient, AuditLogType.Thread, OperationType.Update, entries, newThread.Id.ToString(), newThread.Name);
    }
}
