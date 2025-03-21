﻿using Discord;
using Discord.WebSocket;
using SectomSharp.Data.Enums;

namespace SectomSharp.Events;

public partial class DiscordEvent
{
    private static async Task HandleThreadAlteredAsync(SocketThreadChannel thread, OperationType operationType)
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

    public static async Task HandleThreadCreatedAsync(SocketThreadChannel thread) => await HandleThreadAlteredAsync(thread, OperationType.Create);

    public static async Task HandleThreadDeleteAsync(Cacheable<SocketThreadChannel, ulong> partialThread)
        => await HandleThreadAlteredAsync(await partialThread.GetOrDownloadAsync(), OperationType.Delete);

    public static async Task HandleThreadUpdatedAsync(Cacheable<SocketThreadChannel, ulong> oldPartialThread, SocketThreadChannel newThread)
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
