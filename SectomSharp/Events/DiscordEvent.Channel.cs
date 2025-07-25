using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using JetBrains.Annotations;
using SectomSharp.Data.Enums;
using SectomSharp.Extensions;
using SectomSharp.Utils;
using MentionUtils = SectomSharp.Utils.MentionUtils;

namespace SectomSharp.Events;

public sealed partial class DiscordEvent
{
    private static bool TryGetGuildChannel(SocketChannel socketChannel, [MaybeNullWhen(false)] out SocketGuildChannel guildChannel)
    {
        if (socketChannel is not SocketGuildChannel value
         || socketChannel.GetChannelType() is null or ChannelType.GuildDirectory or ChannelType.Store or ChannelType.PrivateThread or ChannelType.PublicThread)
        {
            guildChannel = null;
            return false;
        }

        guildChannel = value;
        return true;
    }

    private static ChannelDetails GetChannelDetails(SocketGuildChannel channel)
    {
        string name = channel.Name;
        int position = channel.Position;
        ImmutableArray<Overwrite> overwrites = [..channel.PermissionOverwrites];

        return channel switch
        {
            IVoiceChannel voice => new ChannelDetails(name, position, ChannelType.Voice, overwrites, voice.CategoryId, Bitrate: voice.Bitrate, UserLimit: voice.UserLimit),
            IForumChannel forum => new ChannelDetails(name, position, ChannelType.Forum, overwrites, forum.CategoryId, IsNsfw: forum.IsNsfw),
            ITextChannel text => new ChannelDetails(name, position, channel.ChannelType, overwrites, text.CategoryId, text.Topic, text.IsNsfw, text.SlowModeInterval),
            _ => new ChannelDetails(name, position, channel.ChannelType, overwrites)
        };
    }

    private readonly ConcurrentDictionary<ulong, AtomicFirstAndLast<ChannelOverwriteUpdateChange>> _bursts = new();

    private async Task HandleChannelAlteredAsync(SocketChannel socketChannel, OperationType operationType)
    {
        if (!TryGetGuildChannel(socketChannel, out SocketGuildChannel? guildChannel))
        {
            return;
        }

        ChannelDetails details = GetChannelDetails(guildChannel);

        var builders = new List<EmbedFieldBuilder>(12)
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

        ReadOnlySpan<Overwrite> overwrites = details.Overwrites.AsSpan();
        if (!overwrites.IsEmpty)
        {
            foreach (ref readonly Overwrite overwrite in overwrites)
            {
                OverwritePermissions permissions = overwrite.Permissions;
                ulong allowed = permissions.AllowValue;
                ulong denied = permissions.DenyValue;
                if ((allowed | denied) == 0)
                {
                    continue;
                }

                ulong targetId = overwrite.TargetId;
                builders.Add(
                    EmbedFieldBuilderFactory.CreateTruncated(targetId.ToString(), BufferWriter.FormatPermissionDisplayEfficient(allowed, denied, 0, overwrite.TargetType, targetId))
                );
            }
        }

        if (builders.Count == 0)
        {
            return;
        }

        using DiscordWebhookClient? webhookClient = await GetDiscordWebhookClientAsync(guildChannel.Guild.Id, AuditLogType.Channel);
        if (webhookClient is null)
        {
            return;
        }

        await LogAsync(guildChannel.Guild, webhookClient, AuditLogType.Channel, operationType, builders, guildChannel.Id, guildChannel.Name);
    }

    [SkipLocalsInit]
    private async Task FlushBurstAsync(ulong channelId, ImmutableArray<Overwrite> before, ImmutableArray<Overwrite> after, string botUsername, string avatarUrl, long ticks)
    {
        if (await _client.GetChannelAsync(channelId) is not IGuildChannel channel)
        {
            return;
        }

        ulong footerPrefix = channel.Id;
        string channelName = channel.Name;

        int footerPrefixLength = FormattingUtils.CountDigits(null, footerPrefix);
        const string footerSuffix = $" | {nameof(AuditLogType.Channel) + nameof(OperationType.Update)}";
        int footerTotalLength = footerPrefixLength + footerSuffix.Length;
        string footerText = StringUtils.FastAllocateString(null, footerTotalLength);
        {
            ref char start = ref StringUtils.GetFirstChar(footerText);
            ref char current = ref start;

            current = ref MentionUtils.WriteSnowflakeId(ref current, footerPrefix, footerPrefixLength);
            current = ref StringUtils.CopyTo(ref current, footerSuffix);

            ref char end = ref Unsafe.Add(ref start, footerTotalLength);
            Debug.Assert(Unsafe.AreSame(ref current, ref end));
        }

        var embeds = new List<Embed>(DiscordConfig.MaxEmbedsPerMessage);
        var embedFieldBuilders = new List<EmbedFieldBuilder>(EmbedBuilder.MaxFieldCount);
        var embedBuilder = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder { Name = channelName },
            Color = Color.Orange,
            Fields = embedFieldBuilders,
            Footer = new EmbedFooterBuilder { Text = footerText },
            Timestamp = new DateTimeOffset(ticks, TimeSpan.Zero)
        };

        int embedBuilderInitialChars = channelName.Length + footerTotalLength;
        int totalChars = embedBuilderInitialChars;

        Dictionary<ulong, Overwrite> beforeLookup = before.ToDictionary(o => o.TargetId);
        Dictionary<ulong, Overwrite> afterLookup = after.ToDictionary(o => o.TargetId);

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
                        ulong currAllow = afterOverwrite.Permissions.AllowValue;
                        ulong currDeny = afterOverwrite.Permissions.DenyValue;
                        ulong prevAllow = beforeOverwrite.Permissions.AllowValue;
                        ulong prevDeny = beforeOverwrite.Permissions.DenyValue;

                        ulong added = currAllow & ~prevAllow;
                        ulong removed = currDeny & ~prevDeny;
                        ulong reset = (prevAllow | prevDeny) & ~(currAllow | currDeny);

                        if ((added | removed | reset) == 0)
                        {
                            continue;
                        }

                        value = BufferWriter.FormatPermissionDisplayEfficient(added, removed, reset, afterOverwrite.TargetType, targetId);
                        key = targetId.ToString();
                        break;
                    }

                case true:
                    {
                        OverwritePermissions removedPermissions = beforeOverwrite.Permissions;
                        ulong allowed = removedPermissions.AllowValue;
                        ulong denied = removedPermissions.DenyValue;
                        if ((allowed | denied) == 0)
                        {
                            continue;
                        }

                        value = BufferWriter.FormatPermissionDisplayEfficient(allowed, denied, 0, beforeOverwrite.TargetType, targetId);

                        const string suffix = " (Removed)";
                        int digits = FormattingUtils.CountDigits(null, targetId);
                        int totalLength = digits + suffix.Length;
                        key = StringUtils.FastAllocateString(null, totalLength);
                        {
                            ref char start = ref StringUtils.GetFirstChar(key);
                            ref char current = ref start;
                            current = ref MentionUtils.WriteSnowflakeId(ref current, targetId, digits);
                            current = ref StringUtils.CopyTo(ref current, suffix);

                            ref char end = ref Unsafe.Add(ref start, totalLength);
                            Debug.Assert(Unsafe.AreSame(ref current, ref end));
                        }

                        break;
                    }

                default:
                    {
                        OverwritePermissions addedPermissions = afterOverwrite.Permissions;
                        ulong allowed = addedPermissions.AllowValue;
                        ulong denied = addedPermissions.DenyValue;
                        if ((allowed | denied) == 0)
                        {
                            continue;
                        }

                        value = BufferWriter.FormatPermissionDisplayEfficient(allowed, denied, 0, afterOverwrite.TargetType, targetId);

                        const string suffix = " (Added)";
                        int digits = FormattingUtils.CountDigits(null, targetId);
                        int totalLength = digits + suffix.Length;
                        key = StringUtils.FastAllocateString(null, totalLength);
                        {
                            ref char start = ref StringUtils.GetFirstChar(key);
                            ref char current = ref start;
                            current = ref MentionUtils.WriteSnowflakeId(ref current, targetId, digits);
                            current = ref StringUtils.CopyTo(ref current, suffix);

                            ref char end = ref Unsafe.Add(ref start, totalLength);
                            Debug.Assert(Unsafe.AreSame(ref current, ref end));
                        }

                        break;
                    }
            }

            EmbedFieldBuilder embedFieldBuilder = EmbedFieldBuilderFactory.Create(key, value);

            int fieldLength = key.Length + value.Length;
            totalChars += fieldLength;
            if (totalChars >= EmbedBuilder.MaxEmbedLength)
            {
                Embed embed = embedBuilder.Build();
                embeds.Add(embed);

                embedFieldBuilders.Clear();
                embedFieldBuilders.Add(embedFieldBuilder);
                totalChars = embedBuilderInitialChars + fieldLength;
            }
            else
            {
                embedFieldBuilders.Add(embedFieldBuilder);
                if (embedFieldBuilders.Count != EmbedBuilder.MaxFieldCount)
                {
                    continue;
                }

                Embed embed = embedBuilder.Build();
                embeds.Add(embed);
                embedFieldBuilders.Clear();
                totalChars = embedBuilderInitialChars;
            }
        }

        if (embedFieldBuilders.Count > 0)
        {
            Embed embed = embedBuilder.Build();
            embeds.Add(embed);
        }

        if (embeds.Count == 0)
        {
            return;
        }

        using DiscordWebhookClient? webhook = await GetDiscordWebhookClientAsync(channel.GuildId, AuditLogType.Channel);
        if (webhook is null)
        {
            return;
        }

        foreach (Embed[] batch in embeds.Chunk(DiscordConfig.MaxEmbedsPerMessage))
        {
            await webhook.SendMessageAsync(username: botUsername, avatarUrl: avatarUrl, embeds: batch);
        }
    }

    internal async Task RunFlushLoopAsync()
    {
        try
        {
            SocketSelfUser currentUser = _client.CurrentUser;
            string username = currentUser.Username;
            string avatarUrl = currentUser.GetAvatarUrl();

            const long burstSeconds = 3;
            const long burstTimeoutTicks = TimeSpan.TicksPerSecond * burstSeconds;
            var timer = new PeriodicTimer(TimeSpan.FromTicks(burstTimeoutTicks));
            var concurrencyGate = new SemaphoreSlim(50);
            var tasks = new List<Task>(capacity: 128);

            while (await timer.WaitForNextTickAsync())
            {
                long now = DateTime.UtcNow.Ticks;
                foreach ((ulong channelId, AtomicFirstAndLast<ChannelOverwriteUpdateChange> burst) in _bursts)
                {
                    if (now - burst.LastModifiedTicks < burstTimeoutTicks
                     || !_bursts.TryRemove(channelId, out AtomicFirstAndLast<ChannelOverwriteUpdateChange>? atomicFirstAndLast)
                     || !atomicFirstAndLast.TryTakeAndClear(out ChannelOverwriteUpdateChange? first, out ChannelOverwriteUpdateChange? last))
                    {
                        continue;
                    }

                    await concurrencyGate.WaitAsync();
                    tasks.Add(RunFlushAsync(channelId, first, last, username, avatarUrl, concurrencyGate, now, this));
                    continue;

                    static async Task RunFlushAsync(
                        ulong channelId,
                        ChannelOverwriteUpdateChange first,
                        ChannelOverwriteUpdateChange last,
                        string username,
                        string avatarUrl,
                        SemaphoreSlim concurrencyGate,
                        long now,
                        DiscordEvent discord
                    )
                    {
                        try
                        {
                            await discord.FlushBurstAsync(channelId, first.Before, last.After, username, avatarUrl, now);
                        }
                        catch (Exception ex)
                        {
                            discord._logger.DiscordNetUnhandledException(ex.Message, ex);
                        }
                        finally
                        {
                            concurrencyGate.Release();
                        }
                    }
                }

                await Task.WhenAll(CollectionsMarshal.AsSpan(tasks));
                tasks.Clear();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.DiscordNetUnhandledException(ex.Message, ex);
        }
    }

    public Task HandleChannelCreatedAsync(SocketChannel socketChannel) => HandleChannelAlteredAsync(socketChannel, OperationType.Create);

    public Task HandleChannelDestroyedAsync(SocketChannel socketChannel) => HandleChannelAlteredAsync(socketChannel, OperationType.Delete);

    public async Task HandleChannelUpdatedAsync(SocketChannel oldSocketChannel, SocketChannel newSocketChannel)
    {
        if (!(TryGetGuildChannel(oldSocketChannel, out SocketGuildChannel? oldChannel) && TryGetGuildChannel(newSocketChannel, out SocketGuildChannel? newChannel))
         || oldChannel.Position != newChannel.Position)
        {
            return;
        }

        // ReSharper disable UseDeconstruction
        ChannelDetails before = GetChannelDetails(oldChannel);
        ChannelDetails after = GetChannelDetails(newChannel);
        // ReSharper restore UseDeconstruction

        var builders = new List<EmbedFieldBuilder>(12);
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

        SocketGuild guild = newChannel.Guild;

        ImmutableArray<Overwrite> beforeOverwrites = before.Overwrites;
        ImmutableArray<Overwrite> afterOverwrites = after.Overwrites;
        if (!beforeOverwrites.IsDefaultOrEmpty || !afterOverwrites.IsDefaultOrEmpty)
        {
            ulong key = newChannel.Id;

            AtomicFirstAndLast<ChannelOverwriteUpdateChange> queue = _bursts.GetOrAdd(key, _ => new AtomicFirstAndLast<ChannelOverwriteUpdateChange>());
            queue.Push(new ChannelOverwriteUpdateChange(beforeOverwrites, afterOverwrites));
        }

        if (builders.Count == 0)
        {
            return;
        }

        using DiscordWebhookClient? webhookClient = await GetDiscordWebhookClientAsync(guild.Id, AuditLogType.Channel);
        if (webhookClient is null)
        {
            return;
        }

        await LogAsync(guild, webhookClient, AuditLogType.Channel, OperationType.Update, builders, newChannel.Id, newChannel.Name);
    }

    private sealed record ChannelOverwriteUpdateChange(ImmutableArray<Overwrite> Before, ImmutableArray<Overwrite> After);

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

    private sealed class AtomicFirstAndLast<T>
        where T : class
    {
        private T? _first;
        private T? _last;
        private long _lastModifiedTicks = DateTimeOffset.UtcNow.Ticks;

        public long LastModifiedTicks => Interlocked.Read(ref _lastModifiedTicks);

        public void Push(T item)
        {
            Interlocked.CompareExchange(ref _first, item, null);
            Interlocked.Exchange(ref _last, item);
            Interlocked.Exchange(ref _lastModifiedTicks, DateTime.UtcNow.Ticks);
        }

        public bool TryTakeAndClear([NotNullWhen(true)] out T? first, [NotNullWhen(true)] out T? last)
        {
            first = Interlocked.Exchange(ref _first, null);
            last = Interlocked.Exchange(ref _last, null);

            return first is not null || last is not null;
        }
    }

    private static class BufferWriter
    {
        private const string AllowedPrefix = "**Allowed:** ";
        private const string DeniedPrefix = "**Denied:** ";
        private const string ResetPrefix = "**Reset:** ";

        private const string MentionPrefix = "**Mention:** ";
        private const string Separator = ", ";

        private static readonly string?[] PermissionNamesByBitIndex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SkipLocalsInit]
        [MustUseReturnValue]
        private static ref char WritePermissionList(ref char destination, ulong allowed, scoped ref string? firstName)
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
                        destination = ref StringUtils.CopyTo(ref destination, Separator);
                    }
                    else
                    {
                        first = false;
                    }

                    destination = ref StringUtils.CopyTo(ref destination, source);
                }

                allowed &= allowed - 1;
            }

            return ref destination;
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

            totalSize += Separator.Length * (totalElements - 1);
            return totalSize;
        }

        [SkipLocalsInit]
        public static string FormatPermissionDisplayEfficient(ulong allowed, ulong denied, ulong reset, PermissionTarget targetType, ulong targetId)
        {
            Debug.Assert(allowed != 0 || denied != 0 || reset != 0);

            ref string? firstName = ref MemoryMarshal.GetArrayDataReference(PermissionNamesByBitIndex);

            int targetIdLength = FormattingUtils.CountDigits(null, targetId);

            int mentionLength = targetType == PermissionTarget.Role
                ? MentionPrefix.Length + MentionUtils.RoleMentionStart.Length + targetIdLength + 1
                : MentionPrefix.Length + MentionUtils.UserMentionStart.Length + targetIdLength + 1;

            ReadOnlySpan<int> prefixLengthLookupTable =
            [
                0,                                                                  // 0b000 = 0
                AllowedPrefix.Length,                                               // 0b001 = (1 << 0)
                DeniedPrefix.Length,                                                // 0b010 = (1 << 1)
                AllowedPrefix.Length + DeniedPrefix.Length + 1,                     // 0b011 = (1 << 0) | (1 << 1)
                ResetPrefix.Length,                                                 // 0b100 = (1 << 2)
                AllowedPrefix.Length + ResetPrefix.Length + 1,                      // 0b101 = (1 << 0) | (1 << 2)
                DeniedPrefix.Length + ResetPrefix.Length + 1,                       // 0b110 = (1 << 1) | (1 << 2)
                AllowedPrefix.Length + DeniedPrefix.Length + ResetPrefix.Length + 2 // 0b111 = (1 << 0) | (1 << 1) | (1 << 2)
            ];

            int flags = (allowed != 0 ? (int)PermissionFlag.Allowed : 0) | (denied != 0 ? (int)PermissionFlag.Denied : 0) | (reset != 0 ? (int)PermissionFlag.Reset : 0);

            int prefixLength = Unsafe.Add(ref MemoryMarshal.GetReference(prefixLengthLookupTable), flags);

            int permissionsLength = prefixLength
                                  + (allowed != 0 ? CountPermissionListLength(allowed, ref firstName) : 0)
                                  + (denied != 0 ? CountPermissionListLength(denied, ref firstName) : 0)
                                  + (reset != 0 ? CountPermissionListLength(reset, ref firstName) : 0);

            int totalLength = mentionLength + 1 + permissionsLength;

            string buffer = StringUtils.FastAllocateString(null, totalLength);
            ref char start = ref StringUtils.GetFirstChar(buffer);
            ref char current = ref start;

            if (targetType == PermissionTarget.Role)
            {
                current = ref StringUtils.CopyTo(ref current, MentionPrefix + MentionUtils.RoleMentionStart);
            }
            else
            {
                current = ref StringUtils.CopyTo(ref current, MentionPrefix + MentionUtils.UserMentionStart);
            }

            targetId.TryFormat(MemoryMarshal.CreateSpan(ref current, targetIdLength), out _);
            current = ref Unsafe.Add(ref current, targetIdLength);

            current = ref StringUtils.CopyTo(ref current, $"{MentionUtils.MentionEnd}\n");

            switch (flags)
            {
                case (int)PermissionFlag.Allowed:
                    current = ref StringUtils.CopyTo(ref current, AllowedPrefix);
                    current = ref WritePermissionList(ref current, allowed, ref firstName);
                    break;
                case (int)PermissionFlag.Denied:
                    current = ref StringUtils.CopyTo(ref current, DeniedPrefix);
                    current = ref WritePermissionList(ref current, denied, ref firstName);
                    break;
                case (int)PermissionFlag.Allowed | (int)PermissionFlag.Denied:
                    current = ref StringUtils.CopyTo(ref current, AllowedPrefix);
                    current = ref WritePermissionList(ref current, allowed, ref firstName);

                    current = ref StringUtils.CopyTo(ref current, $"\n{DeniedPrefix}");
                    current = ref WritePermissionList(ref current, denied, ref firstName);
                    break;
                case (int)PermissionFlag.Reset:
                    current = ref StringUtils.CopyTo(ref current, ResetPrefix);
                    current = ref WritePermissionList(ref current, reset, ref firstName);
                    break;
                case (int)PermissionFlag.Allowed | (int)PermissionFlag.Reset:
                    current = ref StringUtils.CopyTo(ref current, AllowedPrefix);
                    current = ref WritePermissionList(ref current, allowed, ref firstName);

                    current = ref StringUtils.CopyTo(ref current, $"\n{ResetPrefix}");
                    current = ref WritePermissionList(ref current, reset, ref firstName);
                    break;
                case (int)PermissionFlag.Denied | (int)PermissionFlag.Reset:
                    current = ref StringUtils.CopyTo(ref current, DeniedPrefix);
                    current = ref WritePermissionList(ref current, denied, ref firstName);

                    current = ref StringUtils.CopyTo(ref current, $"\n{ResetPrefix}");
                    current = ref WritePermissionList(ref current, reset, ref firstName);
                    break;
                case (int)PermissionFlag.Allowed | (int)PermissionFlag.Denied | (int)PermissionFlag.Reset:
                    current = ref StringUtils.CopyTo(ref current, AllowedPrefix);
                    current = ref WritePermissionList(ref current, allowed, ref firstName);

                    current = ref StringUtils.CopyTo(ref current, $"\n{DeniedPrefix}");
                    current = ref WritePermissionList(ref current, denied, ref firstName);

                    current = ref StringUtils.CopyTo(ref current, $"\n{ResetPrefix}");
                    current = ref WritePermissionList(ref current, reset, ref firstName);
                    break;
            }

            ref char end = ref Unsafe.Add(ref start, totalLength);
            Debug.Assert(Unsafe.AreSame(ref current, ref end));

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

        [Flags]
        private enum PermissionFlag
        {
            Allowed = 1 << 0,
            Denied = 1 << 1,
            Reset = 1 << 2
        }
    }
}
