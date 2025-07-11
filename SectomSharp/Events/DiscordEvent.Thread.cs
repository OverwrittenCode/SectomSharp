using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using SectomSharp.Data.Enums;
using SectomSharp.Utils;

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

        await LogAsync(
            thread.Guild,
            webhookClient,
            AuditLogType.Thread,
            operationType,
            [
                EmbedFieldBuilderFactory.Create("Id", thread.Id),
                EmbedFieldBuilderFactory.Create("Name", thread.Name),
                EmbedFieldBuilderFactory.Create("Type", thread.Type),
                EmbedFieldBuilderFactory.Create("Parent", $"<#{thread.ParentChannel.Id}>"),
                EmbedFieldBuilderFactory.Create("Topic", thread.Topic)
            ],
            thread.Id,
            thread.Name
        );
    }

    public async Task HandleThreadCreatedAsync(SocketThreadChannel thread) => await HandleThreadAlteredAsync(thread, OperationType.Create);

    public async Task HandleThreadDeleteAsync(Cacheable<SocketThreadChannel, ulong> partialThread)
        => await HandleThreadAlteredAsync(await partialThread.GetOrDownloadAsync(), OperationType.Delete);

    public async Task HandleThreadUpdatedAsync(Cacheable<SocketThreadChannel, ulong> oldPartialThread, SocketThreadChannel newThread)
    {
        SocketThreadChannel oldThread = await oldPartialThread.GetOrDownloadAsync();

        List<EmbedFieldBuilder> builders = new(4);
        AddIfChanged(builders, "Name", oldThread.Name, newThread.Name);
        AddIfChanged(builders, "Type", oldThread.Type, newThread.Type);
        if (oldThread.ParentChannel.Id != newThread.ParentChannel.Id)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("Parent", GetChangeEntry($"<#{oldThread.ParentChannel.Id}>", $"<#{newThread.ParentChannel.Id}>")));
        }

        AddIfChanged(builders, "Topic", oldThread.Topic, newThread.Topic);
        if (builders.Count == 0)
        {
            return;
        }

        using DiscordWebhookClient? webhookClient = await GetDiscordWebhookClientAsync(newThread.Guild, AuditLogType.Thread);
        if (webhookClient is null)
        {
            return;
        }

        await LogAsync(newThread.Guild, webhookClient, AuditLogType.Thread, OperationType.Update, builders, newThread.Id, newThread.Name);
    }
}
