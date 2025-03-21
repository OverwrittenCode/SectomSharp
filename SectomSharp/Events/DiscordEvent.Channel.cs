using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.WebSocket;
using SectomSharp.Data.Enums;

namespace SectomSharp.Events;

public partial class DiscordEvent
{
    private static bool TryGetGuildChannel(SocketChannel socketChannel, [MaybeNullWhen(false)] out IGuildChannel guildChannel)
    {
        guildChannel = null;
        ChannelType? channelType = socketChannel.GetChannelType();

        if (socketChannel is not IGuildChannel value
         || channelType is null or ChannelType.GuildDirectory or ChannelType.Store or ChannelType.PrivateThread or ChannelType.PublicThread)
        {
            return false;
        }

        guildChannel = value;
        return true;
    }

    private static ChannelDetails GetChannelDetails(IGuildChannel channel)
        => channel switch
        {
            IVoiceChannel voice => new ChannelDetails(
                channel.Name,
                channel.Position,
                ChannelType.Voice,
                voice.CategoryId,
                Bitrate: voice.Bitrate,
                UserLimit: voice.UserLimit,
                Overwrites: channel.PermissionOverwrites
            ),

            IForumChannel forum => new ChannelDetails(
                channel.Name,
                channel.Position,
                ChannelType.Forum,
                forum.CategoryId,
                IsNsfw: forum.IsNsfw,
                Overwrites: channel.PermissionOverwrites
            ),

            ITextChannel text => new ChannelDetails(
                channel.Name,
                channel.Position,
                channel.GetChannelType() ?? ChannelType.Text,
                text.CategoryId,
                text.Topic,
                text.IsNsfw,
                text.SlowModeInterval,
                Overwrites: channel.PermissionOverwrites
            ),

            _ => new ChannelDetails(channel.Name, channel.Position, channel.GetChannelType() ?? ChannelType.Text, Overwrites: channel.PermissionOverwrites)
        };

    private static string GetOverwriteTargetDisplay(Overwrite overwrite)
    {
        string mention = overwrite.TargetType == PermissionTarget.User ? MentionUtils.MentionUser(overwrite.TargetId) : MentionUtils.MentionRole(overwrite.TargetId);

        return $"{Format.Bold("Mention:")} {mention}";
    }

    private static string FormatPermissionLists(List<ChannelPermission> allowed, List<ChannelPermission> denied)
    {
        List<string> parts = [];

        if (allowed.Count > 0)
        {
            parts.Add($"{Format.Bold("Allowed:")} {String.Join(", ", allowed)}");
        }

        if (denied.Count > 0)
        {
            parts.Add($"{Format.Bold("Denied:")} {String.Join(", ", denied)}");
        }

        return String.Join("\n", parts);
    }

    private static async Task HandleChannelAlteredAsync(
        SocketChannel socketChannel,
        OperationType operationType
    )
    {
        if (!TryGetGuildChannel(socketChannel, out IGuildChannel? guildChannel))
        {
            return;
        }

        ChannelDetails details = GetChannelDetails(guildChannel);

        List<AuditLogEntry> entries =
        [
            new("Name", details.Name),
            new("Type", details.Type),
            new("Position", details.Position)
        ];

        if (details.CategoryId is { } categoryId)
        {
            entries.Add(new AuditLogEntry("Category", categoryId));
        }

        if (!String.IsNullOrEmpty(details.Topic))
        {
            entries.Add(new AuditLogEntry("Topic", details.Topic));
        }

        if (details.IsNsfw)
        {
            entries.Add(new AuditLogEntry("NSFW", true));
        }

        if (details.SlowMode is { } slowmode and > 0)
        {
            entries.Add(new AuditLogEntry("Slowmode", TimeSpan.FromSeconds(slowmode)));
        }

        if (details.Bitrate is { } bitrate)
        {
            entries.Add(new AuditLogEntry("Bitrate", bitrate));
        }

        if (details.UserLimit is { } userLimit)
        {
            entries.Add(new AuditLogEntry("User Limit", userLimit));
        }

        if (details.Overwrites is not null)
        {
            entries.AddRange(
                from overwrite in details.Overwrites
                let value = FormatPermissionLists(overwrite.Permissions.ToAllowList(), overwrite.Permissions.ToDenyList())
                where !String.IsNullOrEmpty(value)
                select new AuditLogEntry(
                    overwrite.TargetId.ToString(),
                    $"""
                     {GetOverwriteTargetDisplay(overwrite)}
                     {value}
                     """
                )
            );
        }

        await LogAsync(guildChannel.Guild, AuditLogType.Channel, operationType, entries, guildChannel.Id.ToString(), guildChannel.Name);
    }

    public static async Task HandleChannelCreatedAsync(SocketChannel socketChannel) => await HandleChannelAlteredAsync(socketChannel, OperationType.Create);

    public static async Task HandleChannelDestroyedAsync(SocketChannel socketChannel) => await HandleChannelAlteredAsync(socketChannel, OperationType.Delete);

    public static async Task HandleChannelUpdatedAsync(
        SocketChannel oldSocketChannel,
        SocketChannel newSocketChannel
    )
    {
        if (!(TryGetGuildChannel(oldSocketChannel, out IGuildChannel? oldChannel) && TryGetGuildChannel(newSocketChannel, out IGuildChannel? newChannel))
         || oldChannel.Position != newChannel.Position)
        {
            return;
        }

        ChannelDetails before = GetChannelDetails(oldChannel);
        ChannelDetails after = GetChannelDetails(newChannel);

        List<AuditLogEntry> entries =
        [
            new("Name", GetChangeEntry(before.Name, after.Name), before.Name != after.Name),
            new("Category", GetChangeEntry(before.CategoryId, after.CategoryId), before.CategoryId != after.CategoryId),
            new("Topic", GetChangeEntry(before.Topic, after.Topic), before.Topic != after.Topic),
            new("NSFW", after.IsNsfw, before.IsNsfw != after.IsNsfw),
            new(
                "Slowmode",
                GetChangeEntry(
                    before.SlowMode is { } beforeSlowMode ? TimeSpan.FromSeconds(beforeSlowMode) : null,
                    after.SlowMode is { } afterSlowMode ? TimeSpan.FromSeconds(afterSlowMode) : null
                ),
                before.SlowMode != after.SlowMode
            ),
            new("Bitrate", GetChangeEntry(before.Bitrate, after.Bitrate), before.Bitrate != after.Bitrate),
            new("User Limit", GetChangeEntry(before.UserLimit, after.UserLimit), before.UserLimit != after.UserLimit)
        ];

        if ((before.Overwrites, after.Overwrites) is (not null, not null))
        {
            List<AuditLogEntry> overwriteChanges = [];

            IEnumerable<Overwrite> mergedOverwrites = after.Overwrites.UnionBy(before.Overwrites, overwrite => overwrite.TargetId);

            foreach (Overwrite overwrite in mergedOverwrites)
            {
                string key = overwrite.TargetId.ToString();

                Overwrite? beforeOverwrite = before.Overwrites.FirstOrDefault(o => o.TargetId == overwrite.TargetId);

                Overwrite? afterOverwrite = after.Overwrites.FirstOrDefault(o => o.TargetId == overwrite.TargetId);

                key += (beforeOverwrite, afterOverwrite) switch
                {
                    (null, not null) => " (Added)",
                    (not null, null) => " (Removed)",
                    _ => ""
                };

                string value = "";
                switch (beforeOverwrite, afterOverwrite)
                {
                    case ({ } prev, { } curr):
                        List<ChannelPermission> beforeAllowed = prev.Permissions.ToAllowList();
                        List<ChannelPermission> afterAllowed = curr.Permissions.ToAllowList();
                        List<ChannelPermission> newlyAllowed = afterAllowed.Except(beforeAllowed).ToList();
                        List<ChannelPermission> newlyDenied = beforeAllowed.Except(afterAllowed).ToList();

                        value = FormatPermissionLists(newlyAllowed, newlyDenied);

                        break;
                    case (null, { } added):
                        value = FormatPermissionLists(added.Permissions.ToAllowList(), added.Permissions.ToDenyList());

                        break;
                    case ({ } removed, null):
                        value = FormatPermissionLists(removed.Permissions.ToAllowList(), removed.Permissions.ToDenyList());

                        break;
                }

                AuditLogEntry entry = new(
                    key,
                    $"""
                     {GetOverwriteTargetDisplay(overwrite)}
                     {value}
                     """,
                    !String.IsNullOrEmpty(value)
                );

                if (entry.ShouldInclude)
                {
                    overwriteChanges.Add(entry);
                }
            }

            entries.AddRange(overwriteChanges);
        }

        if (!entries.Any(c => c.ShouldInclude))
        {
            return;
        }

        await LogAsync(newChannel.Guild, AuditLogType.Channel, OperationType.Update, entries, newChannel.Id.ToString(), newChannel.Name);
    }

    private readonly record struct ChannelDetails
    (
        string Name,
        int Position,
        ChannelType Type,
        ulong? CategoryId = null,
        string? Topic = null,
        bool IsNsfw = false,
        int? SlowMode = null,
        int? Bitrate = null,
        int? UserLimit = null,
        IReadOnlyCollection<Overwrite>? Overwrites = null
    );
}
