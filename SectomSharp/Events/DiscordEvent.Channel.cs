using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using SectomSharp.Data.Enums;
using SectomSharp.Utils;

namespace SectomSharp.Events;

public sealed partial class DiscordEvent
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
        => overwrite.TargetType == PermissionTarget.User ? $"**Mention:** <@{overwrite.TargetId}>" : $"**Mention:** <@&{overwrite.TargetId}>";

    private static string FormatPermissionLists(List<ChannelPermission> allowed, List<ChannelPermission> denied)
    {
        var parts = new List<string>(2);

        if (allowed.Count > 0)
        {
            parts.Add($"**Allowed:** {String.Join(", ", allowed)}");
        }

        if (denied.Count > 0)
        {
            parts.Add($"**Denied:** {String.Join(", ", denied)}");
        }

        return String.Join('\n', parts);
    }

    private async Task HandleChannelAlteredAsync(SocketChannel socketChannel, OperationType operationType)
    {
        if (!TryGetGuildChannel(socketChannel, out IGuildChannel? guildChannel))
        {
            return;
        }

        ChannelDetails details = GetChannelDetails(guildChannel);

        var builders = new List<EmbedFieldBuilder>(10)
        {
            EmbedFieldBuilderFactory.Create("Name", details.Name),
            EmbedFieldBuilderFactory.Create("Type", details.Type),
            EmbedFieldBuilderFactory.Create("Position", details.Position)
        };
        if (details.CategoryId.HasValue)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("Category", details.CategoryId.Value));
        }

        if (!String.IsNullOrEmpty(details.Topic))
        {
            builders.Add(EmbedFieldBuilderFactory.Create("Topic", details.Topic));
        }

        if (details.IsNsfw)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("NSFW", "True"));
        }

        if (details.SlowMode is { } slowmode and > 0)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("Slowmode", TimeSpan.FromSeconds(slowmode)));
        }

        if (details.Bitrate is { } bitrate)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("Bitrate", bitrate));
        }

        if (details.UserLimit is { } userLimit)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("User Limit", userLimit));
        }

        if (details.Overwrites is not null)
        {
            builders.AddRange(
                from overwrite in details.Overwrites
                let value = FormatPermissionLists(overwrite.Permissions.ToAllowList(), overwrite.Permissions.ToDenyList())
                where !String.IsNullOrEmpty(value)
                select EmbedFieldBuilderFactory.CreateTruncated(
                    overwrite.TargetId.ToString(),
                    $"""
                     {GetOverwriteTargetDisplay(overwrite)}
                     {value}
                     """
                )
            );
        }

        if (builders.Count == 0)
        {
            return;
        }

        using DiscordWebhookClient? webhookClient = await GetDiscordWebhookClientAsync(guildChannel.Guild, AuditLogType.Channel);
        if (webhookClient is null)
        {
            return;
        }

        await LogAsync(guildChannel.Guild, webhookClient, AuditLogType.Channel, operationType, builders, guildChannel.Id, guildChannel.Name);
    }

    public async Task HandleChannelCreatedAsync(SocketChannel socketChannel) => await HandleChannelAlteredAsync(socketChannel, OperationType.Create);

    public async Task HandleChannelDestroyedAsync(SocketChannel socketChannel) => await HandleChannelAlteredAsync(socketChannel, OperationType.Delete);

    public async Task HandleChannelUpdatedAsync(SocketChannel oldSocketChannel, SocketChannel newSocketChannel)
    {
        if (!(TryGetGuildChannel(oldSocketChannel, out IGuildChannel? oldChannel) && TryGetGuildChannel(newSocketChannel, out IGuildChannel? newChannel))
         || oldChannel.Position != newChannel.Position)
        {
            return;
        }

        ChannelDetails before = GetChannelDetails(oldChannel);
        ChannelDetails after = GetChannelDetails(newChannel);

        var builders = new List<EmbedFieldBuilder>(10);
        AddIfChanged(builders, "Name", before.Name, after.Name);
        AddIfChanged(builders, "Category", before.CategoryId, after.CategoryId);
        AddIfChanged(builders, "Topic", before.Topic, after.Topic);
        AddIfChanged(builders, "NSFW", after.IsNsfw, before.IsNsfw != after.IsNsfw);
        if (before.SlowMode != after.SlowMode)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("Slowmode", GetChangeEntry(TimeSpan.FromSeconds(before.SlowMode ?? 0), TimeSpan.FromSeconds(after.SlowMode ?? 0))));
        }

        AddIfChanged(builders, "Bitrate", before.Bitrate, after.Bitrate);
        AddIfChanged(builders, "User Limit", before.UserLimit, after.UserLimit);
        if ((before.Overwrites, after.Overwrites) is (not null, not null))
        {
            IEnumerable<Overwrite> mergedOverwrites = after.Overwrites.UnionBy(before.Overwrites, overwrite => overwrite.TargetId);

            foreach (Overwrite overwrite in mergedOverwrites)
            {
                Overwrite? beforeOverwrite = before.Overwrites.FirstOrDefault(o => o.TargetId == overwrite.TargetId);
                Overwrite? afterOverwrite = after.Overwrites.FirstOrDefault(o => o.TargetId == overwrite.TargetId);

                string? value = (beforeOverwrite, afterOverwrite) switch
                {
                    ({ } prev, { } curr) => FormatPermissionLists(
                        curr.Permissions.ToAllowList().Except(prev.Permissions.ToAllowList()).ToList(),
                        prev.Permissions.ToAllowList().Except(curr.Permissions.ToAllowList()).ToList()
                    ),
                    (null, { } added) => FormatPermissionLists(added.Permissions.ToAllowList(), added.Permissions.ToDenyList()),
                    ({ } removed, null) => FormatPermissionLists(removed.Permissions.ToAllowList(), removed.Permissions.ToDenyList()),
                    _ => null
                };

                if (String.IsNullOrEmpty(value))
                {
                    continue;
                }

                string key = (beforeOverwrite, afterOverwrite) switch
                {
                    (null, not null) => $"{overwrite.TargetId} (Added)",
                    (not null, null) => $"{overwrite.TargetId} (Removed)",
                    _ => overwrite.TargetId.ToString()
                };

                builders.Add(
                    EmbedFieldBuilderFactory.CreateTruncated(
                        key,
                        $"""
                         {GetOverwriteTargetDisplay(overwrite)}
                         {value}
                         """
                    )
                );
            }
        }

        if (builders.Count == 0)
        {
            return;
        }

        using DiscordWebhookClient? webhookClient = await GetDiscordWebhookClientAsync(newChannel.Guild, AuditLogType.Channel);
        if (webhookClient is null)
        {
            return;
        }

        await LogAsync(newChannel.Guild, webhookClient, AuditLogType.Channel, OperationType.Update, builders, newChannel.Id, newChannel.Name);
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
