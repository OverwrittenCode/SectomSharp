using Discord;
using Discord.WebSocket;
using SectomSharp.Data.Enums;
using SectomSharp.Extensions;

namespace SectomSharp.Events;

public partial class DiscordEvent
{
#pragma warning disable CA1822
    public async Task HandleMessageDeletedAsync(Cacheable<IMessage, ulong> partialMessage, Cacheable<IMessageChannel, ulong> _)
#pragma warning restore CA1822
    {
        if (partialMessage is not { Value: { Author.IsBot: false, Channel: IGuildChannel { Guild: { } guild } } message })
        {
            return;
        }

        List<AuditLogEntry> entries =
        [
            new("Channel Id", message.Channel.Id),
            new("Author", message.Author.Mention),
            new("Created At", message.Timestamp.DateTime.GetRelativeTimestamp()),
            new("Content", message.Content)
        ];

        if (message.MentionedChannelIds.Count > 0)
        {
            entries.Add(new AuditLogEntry("Mentioned Channels", String.Join(", ", message.MentionedChannelIds.Select(MentionUtils.MentionChannel))));
        }

        if (message.MentionedRoleIds.Count > 0)
        {
            entries.Add(new AuditLogEntry("Mentioned Roles", String.Join(", ", message.MentionedRoleIds.Select(MentionUtils.MentionRole))));
        }

        if (message.MentionedUserIds.Count > 0)
        {
            entries.Add(new AuditLogEntry("Mentioned Users", String.Join(", ", message.MentionedUserIds.Select(MentionUtils.MentionUser))));
        }

        if (message.MentionedEveryone)
        {
            entries.Add(new AuditLogEntry("Mentioned Everyone", message.MentionedEveryone));
        }

        await LogAsync(guild, AuditLogType.Message, OperationType.Delete, entries, message.Id.ToString(), message.Author.Username, message.Author.GetDisplayAvatarUrl());
    }

#pragma warning disable CA1822
    public async Task HandleMessageUpdatedAsync(Cacheable<IMessage, ulong> oldPartialMessage, SocketMessage newMessage, ISocketMessageChannel _)
#pragma warning restore CA1822
    {
        if (oldPartialMessage is not { Value: { Author.IsBot: false, Channel: IGuildChannel { Guild: { } guild } } oldMessage })
        {
            return;
        }

        List<AuditLogEntry> entries =
        [
            new("Content", GetChangeEntry(oldMessage.Content, newMessage.Content), oldMessage.Content != newMessage.Content)
        ];

        await LogAsync(guild, AuditLogType.Message, OperationType.Update, entries, newMessage.Id.ToString(), newMessage.Author.Username, newMessage.Author.GetDisplayAvatarUrl());
    }
}
