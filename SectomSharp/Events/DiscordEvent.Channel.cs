using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.WebSocket;
using SectomSharp.Data.Enums;

namespace SectomSharp.Events;

public partial class DiscordEvent
{
    private static bool TryGetGuildChannel(
        SocketChannel socketChannel,
        [MaybeNullWhen(false)] out IGuildChannel guildChannel
    )
    {
        guildChannel = null;
        ChannelType? channelType = socketChannel.GetChannelType();

        if (
            socketChannel is not IGuildChannel value
            || channelType
                is null
                    or ChannelType.GuildDirectory
                    or ChannelType.Store
                    or ChannelType.PrivateThread
                    or ChannelType.PublicThread
        )
        {
            return false;
        }

        guildChannel = value;
        return true;
    }

    private static ChannelDetails GetChannelDetails(IGuildChannel channel) =>
        channel switch
        {
            IVoiceChannel voice => new(
                Name: channel.Name,
                Position: channel.Position,
                Type: ChannelType.Voice,
                CategoryId: voice.CategoryId,
                Bitrate: voice.Bitrate,
                UserLimit: voice.UserLimit,
                Overwrites: channel.PermissionOverwrites
            ),

            IForumChannel forum => new(
                Name: channel.Name,
                Position: channel.Position,
                Type: ChannelType.Forum,
                CategoryId: forum.CategoryId,
                IsNsfw: forum.IsNsfw,
                Overwrites: channel.PermissionOverwrites
            ),

            ITextChannel text => new(
                Name: channel.Name,
                Position: channel.Position,
                Type: channel.GetChannelType() ?? ChannelType.Text,
                CategoryId: text.CategoryId,
                Topic: text.Topic,
                IsNsfw: text.IsNsfw,
                SlowMode: text.SlowModeInterval,
                Overwrites: channel.PermissionOverwrites
            ),

            _ => new(
                Name: channel.Name,
                Position: channel.Position,
                Type: channel.GetChannelType() ?? ChannelType.Text,
                Overwrites: channel.PermissionOverwrites
            ),
        };

    private static string GetOverwriteTargetDisplay(Overwrite overwrite)
    {
        var mention =
            overwrite.TargetType == PermissionTarget.User
                ? MentionUtils.MentionUser(overwrite.TargetId)
                : MentionUtils.MentionRole(overwrite.TargetId);

        return $"{Format.Bold("Mention:")} {mention}";
    }

    private static string FormatPermissionLists(
        List<ChannelPermission> allowed,
        List<ChannelPermission> denied
    )
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

#pragma warning disable CA1822 // Mark members as static
    private async Task HandleChannelAlteredAsync(
#pragma warning restore CA1822 // Mark members as static
        SocketChannel socketChannel,
        OperationType operationType
    )
    {
        if (!TryGetGuildChannel(socketChannel, out var guildChannel))
        {
            return;
        }

        var details = GetChannelDetails(guildChannel);

        List<AuditLogEntry> entries =
        [
            new("Name", details.Name),
            new("Type", details.Type),
            new("Position", details.Position),
        ];

        if (details.CategoryId is ulong categoryId)
        {
            entries.Add(new("Category", categoryId));
        }

        if (!String.IsNullOrEmpty(details.Topic))
        {
            entries.Add(new("Topic", details.Topic));
        }

        if (details.IsNsfw)
        {
            entries.Add(new("NSFW", true));
        }

        if (details.SlowMode is int slowmode and > 0)
        {
            entries.Add(new("Slowmode", TimeSpan.FromSeconds(slowmode)));
        }

        if (details.Bitrate is int bitrate)
        {
            entries.Add(new("Bitrate", bitrate));
        }

        if (details.UserLimit is int userLimit)
        {
            entries.Add(new("User Limit", userLimit));
        }

        if (details.Overwrites is not null)
        {
            foreach (Overwrite overwrite in details.Overwrites)
            {
                var value = FormatPermissionLists(
                    overwrite.Permissions.ToAllowList(),
                    overwrite.Permissions.ToDenyList()
                );

                if (!String.IsNullOrEmpty(value))
                {
                    entries.Add(
                        new(
                            overwrite.TargetId.ToString(),
                            $"""
                            {GetOverwriteTargetDisplay(overwrite)}
                            {value}
                            """
                        )
                    );
                }
            }
        }

        await LogAsync(
            guildChannel.Guild,
            AuditLogType.Channel,
            operationType,
            entries,
            footerPrefix: guildChannel.Id.ToString(),
            authorName: guildChannel.Name
        );
    }

    public async Task HandleChannelCreatedAsync(SocketChannel socketChannel) =>
        await HandleChannelAlteredAsync(socketChannel, OperationType.Create);

    public async Task HandleChannelDestroyedAsync(SocketChannel socketChannel) =>
        await HandleChannelAlteredAsync(socketChannel, OperationType.Delete);

#pragma warning disable CA1822 // Mark members as static
    public async Task HandleChannelUpdatedAsync(
#pragma warning restore CA1822 // Mark members as static
        SocketChannel beforeSocket,
        SocketChannel afterSocket
    )
    {
        if (
            !(
                TryGetGuildChannel(beforeSocket, out var beforeChannel)
                && TryGetGuildChannel(afterSocket, out var afterChannel)
            )
            || beforeChannel.Position != afterChannel.Position
        )
        {
            return;
        }

        ChannelDetails before = GetChannelDetails(beforeChannel);
        ChannelDetails after = GetChannelDetails(afterChannel);

        List<AuditLogEntry> entries =
        [
            new("Name", GetChangeEntry(before.Name, after.Name), before.Name != after.Name),
            new(
                "Category",
                GetChangeEntry(before.CategoryId, after.CategoryId),
                before.CategoryId != after.CategoryId
            ),
            new("Topic", GetChangeEntry(before.Topic, after.Topic), before.Topic != after.Topic),
            new("NSFW", after.IsNsfw, before.IsNsfw != after.IsNsfw),
            new(
                "Slowmode",
                GetChangeEntry(
                    before.SlowMode.HasValue ? TimeSpan.FromSeconds(before.SlowMode.Value) : null,
                    after.SlowMode.HasValue ? TimeSpan.FromSeconds(after.SlowMode.Value) : null
                ),
                before.SlowMode != after.SlowMode
            ),
            new(
                "Bitrate",
                GetChangeEntry(before.Bitrate, after.Bitrate),
                before.Bitrate != after.Bitrate
            ),
            new(
                "User Limit",
                GetChangeEntry(before.UserLimit, after.UserLimit),
                before.UserLimit != after.UserLimit
            ),
        ];

        if ((before.Overwrites, after.Overwrites) is (not null, not null))
        {
            List<AuditLogEntry> overwriteChanges = [];

            var mergedOverwrites = after.Overwrites.UnionBy(
                before.Overwrites,
                overwrite => overwrite.TargetId
            );

            foreach (var overwrite in mergedOverwrites)
            {
                string key = overwrite.TargetId.ToString();

                Overwrite? beforeOverwrite = before.Overwrites.FirstOrDefault(o =>
                    o.TargetId == overwrite.TargetId
                );

                Overwrite? afterOverwrite = after.Overwrites.FirstOrDefault(o =>
                    o.TargetId == overwrite.TargetId
                );

                key += (beforeOverwrite, afterOverwrite) switch
                {
                    (null, not null) => " (Added)",
                    (not null, null) => " (Removed)",
                    _ => "",
                };

                string value = "";
                switch (beforeOverwrite, afterOverwrite)
                {
                    case (Overwrite prev, Overwrite curr):
                        var beforeAllowed = prev.Permissions.ToAllowList();
                        var afterAllowed = curr.Permissions.ToAllowList();
                        var newlyAllowed = afterAllowed.Except(beforeAllowed).ToList();
                        var newlyDenied = beforeAllowed.Except(afterAllowed).ToList();

                        value = FormatPermissionLists(newlyAllowed, newlyDenied);

                        break;
                    case (null, Overwrite added):
                        value = FormatPermissionLists(
                            added.Permissions.ToAllowList(),
                            added.Permissions.ToDenyList()
                        );

                        break;
                    case (Overwrite removed, null):
                        value = FormatPermissionLists(
                            removed.Permissions.ToAllowList(),
                            removed.Permissions.ToDenyList()
                        );

                        break;
                }

                AuditLogEntry entry =
                    new(
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

        await LogAsync(
            afterChannel.Guild,
            AuditLogType.Channel,
            OperationType.Update,
            entries,
            footerPrefix: afterChannel.Id.ToString(),
            authorName: afterChannel.Name
        );
    }

    private readonly record struct ChannelDetails(
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
