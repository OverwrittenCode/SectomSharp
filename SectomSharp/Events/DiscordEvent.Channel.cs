using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        if (socketChannel is not IGuildChannel value
         || socketChannel.GetChannelType() is null or ChannelType.GuildDirectory or ChannelType.Store or ChannelType.PrivateThread or ChannelType.PublicThread)
        {
            guildChannel = null;
            return false;
        }

        guildChannel = value;
        return true;
    }

    private static ChannelDetails GetChannelDetails(IGuildChannel channel)
    {
        string name = channel.Name;
        int position = channel.Position;
        ImmutableArray<Overwrite> overwrites = [..channel.PermissionOverwrites];

        return channel switch
        {
            IVoiceChannel voice => new ChannelDetails(name, position, ChannelType.Voice, overwrites, voice.CategoryId, Bitrate: voice.Bitrate, UserLimit: voice.UserLimit),
            IForumChannel forum => new ChannelDetails(name, position, ChannelType.Forum, overwrites, forum.CategoryId, IsNsfw: forum.IsNsfw),
            ITextChannel text => new ChannelDetails(
                name,
                position,
                channel.GetChannelType() ?? ChannelType.Text,
                overwrites,
                text.CategoryId,
                text.Topic,
                text.IsNsfw,
                text.SlowModeInterval
            ),
            _ => new ChannelDetails(name, position, channel.GetChannelType() ?? ChannelType.Text, overwrites)
        };
    }

    private async Task HandleChannelAlteredAsync(SocketChannel socketChannel, OperationType operationType)
    {
        if (!TryGetGuildChannel(socketChannel, out IGuildChannel? guildChannel))
        {
            return;
        }

        ChannelDetails details = GetChannelDetails(guildChannel);
        ReadOnlySpan<Overwrite> overwrites = details.Overwrites.AsSpan();

        var builders = new List<EmbedFieldBuilder>(9 + overwrites.Length)
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

        if (details.SlowMode is > 0)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("Slowmode", TimeSpan.FromSeconds(details.SlowMode.Value)));
        }

        if (details.Bitrate.HasValue)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("Bitrate", details.Bitrate.Value));
        }

        if (details.UserLimit.HasValue)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("User Limit", details.UserLimit.Value));
        }

        if (overwrites.IsEmpty)
        {
            foreach (ref readonly Overwrite overwrite in overwrites)
            {
                OverwritePermissions permissions = overwrite.Permissions;
                ulong allowed = permissions.AllowValue;
                ulong denied = permissions.DenyValue;
                if (allowed == 0 && denied == 0)
                {
                    continue;
                }

                ulong targetId = overwrite.TargetId;
                builders.Add(
                    EmbedFieldBuilderFactory.CreateTruncated(targetId.ToString(), BufferWriter.FormatPermissionDisplayEfficient(allowed, denied, overwrite.TargetType, targetId))
                );
            }
        }

        if (builders.Count == 0)
        {
            return;
        }

        using DiscordWebhookClient? webhookClient = await GetDiscordWebhookClientAsync(guildChannel.GuildId, AuditLogType.Channel);
        if (webhookClient is null)
        {
            return;
        }

        await LogAsync(guildChannel.Guild, webhookClient, AuditLogType.Channel, operationType, builders, guildChannel.Id, guildChannel.Name);
    }

    public Task HandleChannelCreatedAsync(SocketChannel socketChannel) => HandleChannelAlteredAsync(socketChannel, OperationType.Create);

    public Task HandleChannelDestroyedAsync(SocketChannel socketChannel) => HandleChannelAlteredAsync(socketChannel, OperationType.Delete);

    public async Task HandleChannelUpdatedAsync(SocketChannel oldSocketChannel, SocketChannel newSocketChannel)
    {
        if (!(TryGetGuildChannel(oldSocketChannel, out IGuildChannel? oldChannel) && TryGetGuildChannel(newSocketChannel, out IGuildChannel? newChannel))
         || oldChannel.Position != newChannel.Position)
        {
            return;
        }

        // ReSharper disable UseDeconstruction
        ChannelDetails before = GetChannelDetails(oldChannel);
        ChannelDetails after = GetChannelDetails(newChannel);
        // ReSharper restore UseDeconstruction

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

        ImmutableArray<Overwrite> beforeOverwrites = before.Overwrites;
        ImmutableArray<Overwrite> afterOverwrites = after.Overwrites;
        if (!beforeOverwrites.IsDefaultOrEmpty || !afterOverwrites.IsDefaultOrEmpty)
        {
            Dictionary<ulong, Overwrite> beforeLookup = beforeOverwrites.ToDictionary(o => o.TargetId);
            Dictionary<ulong, Overwrite> afterLookup = afterOverwrites.ToDictionary(o => o.TargetId);

            var allTargetIds = new HashSet<ulong>(beforeLookup.Keys);
            allTargetIds.UnionWith(afterLookup.Keys);

            foreach (ulong targetId in allTargetIds)
            {
                bool hasBeforeOverwrite = beforeLookup.TryGetValue(targetId, out Overwrite beforeOverwrite);
                bool hasAfterOverwrite = afterLookup.TryGetValue(targetId, out Overwrite afterOverwrite);

                string value;
                string key;
                switch (hasBeforeOverwrite)
                {
                    case true when hasAfterOverwrite:
                        {
                            ulong currAllowValue = afterOverwrite.Permissions.AllowValue;
                            ulong prevAllowValue = beforeOverwrite.Permissions.AllowValue;
                            ulong allowed = currAllowValue & ~prevAllowValue;
                            ulong denied = prevAllowValue & ~currAllowValue;
                            if (allowed == 0 && denied == 0)
                            {
                                continue;
                            }

                            value = BufferWriter.FormatPermissionDisplayEfficient(allowed, denied, afterOverwrite.TargetType, targetId);
                            key = targetId.ToString();
                            break;
                        }

                    case true:
                        {
                            OverwritePermissions removedPermissions = beforeOverwrite.Permissions;
                            ulong allowed = removedPermissions.AllowValue;
                            ulong denied = removedPermissions.DenyValue;
                            if (allowed == 0 && denied == 0)
                            {
                                continue;
                            }

                            value = BufferWriter.FormatPermissionDisplayEfficient(allowed, denied, beforeOverwrite.TargetType, targetId);
                            key = $"{targetId} (Removed)";
                            break;
                        }

                    default:
                        {
                            OverwritePermissions addedPermissions = afterOverwrite.Permissions;
                            ulong allowed = addedPermissions.AllowValue;
                            ulong denied = addedPermissions.DenyValue;
                            if (allowed == 0 && denied == 0)
                            {
                                continue;
                            }

                            value = BufferWriter.FormatPermissionDisplayEfficient(allowed, denied, afterOverwrite.TargetType, targetId);
                            key = $"{targetId} (Added)";
                            break;
                        }
                }

                builders.Add(EmbedFieldBuilderFactory.CreateTruncated(key, value));
            }
        }

        if (builders.Count == 0)
        {
            return;
        }

        using DiscordWebhookClient? webhookClient = await GetDiscordWebhookClientAsync(newChannel.GuildId, AuditLogType.Channel);
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
        ImmutableArray<Overwrite> Overwrites,
        ulong? CategoryId = null,
        string? Topic = null,
        bool IsNsfw = false,
        int? SlowMode = null,
        int? Bitrate = null,
        int? UserLimit = null
    );

    private static unsafe class BufferWriter
    {
        private const string AllowedPrefix = "**Allowed:** ";
        private const string DeniedPrefix = "**Denied:** ";

        private const string MentionPrefix = "**Mention:** ";
        private const string RoleMentionStart = "<@&";
        private const string UserMentionStart = "<@";
        private const char MentionEnd = '>';

        private static readonly string?[] PermissionNamesByBitIndex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SkipLocalsInit]
        private static void WritePermissionList(ref char* destination, ulong allowed, ref string? firstName)
        {
            bool first = true;
            while (allowed != 0)
            {
                int i = BitOperations.TrailingZeroCount(allowed);
                string? source = Unsafe.Add(ref firstName, i);
                if (source is not null)
                {
                    if (!first)
                    {
                        StringUtils.WriteFixed2(ref destination, ',', ' ');
                    }
                    else
                    {
                        first = false;
                    }

                    StringUtils.CopyTo(ref destination, source);
                }

                allowed &= allowed - 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CountPermissionListLength(ulong value, ref string? firstName)
        {
            int totalSize = 0;
            int totalElements = 0;
            while (value != 0)
            {
                int i = BitOperations.TrailingZeroCount(value);
                string? source = Unsafe.Add(ref firstName, i);

                if (source != null)
                {
                    totalSize += source.Length;
                    totalElements++;
                }

                value &= value - 1;
            }

            totalSize += ", ".Length * (totalElements - 1);
            return totalSize;
        }

        [SkipLocalsInit]
        public static string FormatPermissionDisplayEfficient(ulong allowed, ulong denied, PermissionTarget targetType, ulong targetId)
        {
            Debug.Assert(allowed != 0 || denied != 0);

            ref string? firstName = ref MemoryMarshal.GetArrayDataReference(PermissionNamesByBitIndex);

            int targetIdLength = FormattingUtils.CountDigits(null, targetId);

            int mentionLength = targetType == PermissionTarget.Role
                ? MentionPrefix.Length + RoleMentionStart.Length + targetIdLength + 1
                : MentionPrefix.Length + UserMentionStart.Length + targetIdLength + 1;

            int permissionsLength = allowed != 0
                ? denied != 0
                    ? AllowedPrefix.Length + CountPermissionListLength(allowed, ref firstName) + 1 + DeniedPrefix.Length + CountPermissionListLength(denied, ref firstName)
                    : AllowedPrefix.Length + CountPermissionListLength(allowed, ref firstName)
                : DeniedPrefix.Length + CountPermissionListLength(denied, ref firstName);

            int totalLength = mentionLength + 1 + permissionsLength;

            string buffer = StringUtils.FastAllocateString(null, totalLength);
            fixed (char* bufferPtr = buffer)
            {
                char* ptr = bufferPtr;

                if (targetType == PermissionTarget.Role)
                {
                    StringUtils.CopyTo(ref ptr, MentionPrefix + RoleMentionStart);
                }
                else
                {
                    StringUtils.CopyTo(ref ptr, MentionPrefix + UserMentionStart);
                }

                targetId.TryFormat(new Span<char>(ptr, targetIdLength), out _);
                ptr += targetIdLength;

                StringUtils.WriteFixed2(ref ptr, MentionEnd, '\n');

                if (allowed != 0)
                {
                    StringUtils.CopyTo(ref ptr, AllowedPrefix);
                    WritePermissionList(ref ptr, allowed, ref firstName);
                    if (denied == 0)
                    {
                        return buffer;
                    }

                    *ptr++ = '\n';
                }

                StringUtils.CopyTo(ref ptr, DeniedPrefix);
                WritePermissionList(ref ptr, denied, ref firstName);

                Debug.Assert(ptr == bufferPtr + totalLength);
            }

            return buffer;
        }

        static BufferWriter()
        {
            const int maxBits = 53;
            PermissionNamesByBitIndex = new string?[maxBits];
            foreach (ChannelPermission channelPermission in Enum.GetValues<ChannelPermission>())
            {
                int i = BitOperations.TrailingZeroCount((ulong)channelPermission);
                PermissionNamesByBitIndex[i] = channelPermission.ToString();
            }
        }
    }
}
