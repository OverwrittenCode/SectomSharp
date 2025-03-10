using Discord;
using Discord.WebSocket;
using SectomSharp.Data.Enums;

namespace SectomSharp.Events;

public partial class DiscordEvent
{
#pragma warning disable CA1822
    private async Task HandleThreadAlteredAsync(SocketThreadChannel thread, OperationType operationType)
#pragma warning restore CA1822
    {
        List<AuditLogEntry> entries =
        [
            new("Id", thread.Id),
            new("Name", thread.Name),
            new("Type", thread.Type),
            new("Parent", MentionUtils.MentionChannel(thread.ParentChannel.Id)),
            new("Topic", thread.Topic)
        ];

        await LogAsync(thread.Guild, AuditLogType.Thread, operationType, entries, thread.Id.ToString(), thread.Name);
    }

    public async Task HandleThreadCreatedAsync(SocketThreadChannel thread) => await HandleThreadAlteredAsync(thread, OperationType.Create);

    public async Task HandleThreadDeleteAsync(Cacheable<SocketThreadChannel, ulong> partialThread)
        => await HandleThreadAlteredAsync(await partialThread.GetOrDownloadAsync(), OperationType.Delete);

#pragma warning disable CA1822
    public async Task HandleThreadUpdatedAsync(Cacheable<SocketThreadChannel, ulong> oldPartialThread, SocketThreadChannel newThread)
#pragma warning restore CA1822
    {
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

        await LogAsync(newThread.Guild, AuditLogType.Thread, OperationType.Update, entries, newThread.Id.ToString(), newThread.Name);
    }
}
