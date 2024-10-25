using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SectomSharp.Data;
using SectomSharp.Data.Enums;
using SectomSharp.Data.Models;
using SectomSharp.Managers.Pagination.Builders;
using SectomSharp.Managers.Pagination.Button;
using SectomSharp.Services;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Admin;

public partial class AdminModule
{
    public partial class ConfigModule
    {
        [Group("log-channel", "Log Channel configuration")]
        public sealed class LogChannelModule : BaseModule
        {
            private static readonly BotLogType[] BotLogTypes = Enum.GetValues<BotLogType>();
            private static readonly AuditLogType[] AuditLogTypes = Enum.GetValues<AuditLogType>();

            [SlashCommand("set-bot-log", "Add or modify a bot log channel configuration")]
            public async Task SetBotLog([ComplexParameter] LogChannelOptions<BotLogType> options)
            {
                options.Deconstruct(out var logChannel, out var action, out var reason);

                await DeferAsync();

                using (var db = new ApplicationDbContext())
                {
                    var guild = await db
                        .Guilds.Where(guild => guild.Id == Context.Guild.Id)
                        .Include(guild => guild.BotLogChannels)
                        .SingleOrDefaultAsync();

                    guild ??= (await db.Guilds.AddAsync(new() { Id = Context.Guild.Id })).Entity;

                    var botLogChannel = guild.BotLogChannels.FirstOrDefault(channel =>
                        channel.Id == logChannel.Id
                    );

                    if (botLogChannel is null)
                    {
                        guild.BotLogChannels.Add(
                            new()
                            {
                                Id = logChannel.Id,
                                GuildId = Context.Guild.Id,
                                BotLogType = action,
                            }
                        );
                    }
                    else if (!botLogChannel.BotLogType.HasFlag(action))
                    {
                        botLogChannel.BotLogType |= action;
                    }
                    else
                    {
                        await RespondOrFollowUpAsync(AlreadyConfiguredMessage);
                        return;
                    }

                    await db.SaveChangesAsync();
                }

                await LogAsync(Context, reason, logChannel.Id);
            }

            [SlashCommand("set-audit-log", "Add or modify an audit log channel configuration")]
            [RequireBotPermission(GuildPermission.ViewAuditLog)]
            public async Task SetAuditLog(
                [ComplexParameter] LogChannelOptions<AuditLogType> options
            )
            {
                options.Deconstruct(out var logChannel, out var action, out var reason);

                if (!Context.Guild.CurrentUser.GetPermissions(logChannel).ManageWebhooks)
                {
                    await RespondOrFollowUpAsync(
                        $"Bot requires channel permission {ChannelPermission.ManageWebhooks} in {MentionUtils.MentionChannel(logChannel.Id)}",
                        ephemeral: true
                    );
                }

                await DeferAsync();

                using (var db = new ApplicationDbContext())
                {
                    var guild = await db
                        .Guilds.Where(guild => guild.Id == Context.Guild.Id)
                        .Include(guild => guild.AuditLogChannels)
                        .SingleOrDefaultAsync();

                    guild ??= (await db.Guilds.AddAsync(new() { Id = Context.Guild.Id })).Entity;

                    var auditLogChannel = guild.AuditLogChannels.SingleOrDefault(channel =>
                        channel.Id == logChannel.Id
                    );

                    if (auditLogChannel is null)
                    {
                        var webhook =
                            (await logChannel.GetWebhooksAsync()).FirstOrDefault(webhook =>
                                webhook.Creator.Id == Context.Guild.CurrentUser.Id
                            )
                            ?? (
                                await logChannel.CreateWebhookAsync(
                                    Context.Guild.CurrentUser.DisplayName,
                                    options: DiscordUtils.GetAuditReasonRequestOptions(
                                        Context,
                                        "Automated request for audit logging."
                                    )
                                )
                            );

                        guild.AuditLogChannels.Add(
                            (
                                await db.AuditLogChannels.AddAsync(
                                    new()
                                    {
                                        Id = logChannel.Id,
                                        GuildId = Context.Guild.Id,
                                        WebhookUrl =
                                            $"https://discord.com/api/webhooks/{webhook.Id}/{webhook.Token}",
                                        AuditLogType = action,
                                    }
                                )
                            ).Entity
                        );
                    }
                    else if (!auditLogChannel.AuditLogType.HasFlag(action))
                    {
                        auditLogChannel.AuditLogType |= action;
                    }
                    else
                    {
                        await RespondOrFollowUpAsync(AlreadyConfiguredMessage);
                        return;
                    }

                    await db.SaveChangesAsync();
                }

                await LogAsync(Context, reason, logChannel.Id);
            }

            [SlashCommand("remove-bot-log", "Remove a bot log channel configuration")]
            public async Task RemoveBotLog([ComplexParameter] LogChannelOptions<BotLogType> options)
            {
                options.Deconstruct(out var logChannel, out var action, out var reason);

                await DeferAsync();

                using (var db = new ApplicationDbContext())
                {
                    var guild = await db
                        .Guilds.Where(guild => guild.Id == Context.Guild.Id)
                        .Include(guild => guild.BotLogChannels)
                        .SingleOrDefaultAsync();

                    if (guild is null)
                    {
                        await db.Guilds.AddAsync(new() { Id = Context.Guild.Id });
                        await db.SaveChangesAsync();
                        await RespondOrFollowUpAsync(NotConfiguredMessage);
                        return;
                    }

                    var botLogChannel = guild.BotLogChannels.FirstOrDefault(channel =>
                        channel.Id == options.LogChannel.Id
                    );

                    if (botLogChannel is null)
                    {
                        await RespondOrFollowUpAsync(NotConfiguredMessage);
                        return;
                    }

                    if (botLogChannel.BotLogType == action)
                    {
                        guild.BotLogChannels.Remove(botLogChannel);
                    }
                    else if (botLogChannel.BotLogType.HasFlag(action))
                    {
                        botLogChannel.BotLogType &= ~action;
                    }
                    else
                    {
                        await RespondOrFollowUpAsync(NotConfiguredMessage);
                        return;
                    }

                    await db.SaveChangesAsync();
                }

                await LogAsync(Context, options.Reason, options.LogChannel.Id);
            }

            [SlashCommand("remove-audit-log", "Remove an audit log channel configuration")]
            public async Task RemoveAuditLog(
                [ComplexParameter] LogChannelOptions<AuditLogType> options
            )
            {
                options.Deconstruct(out var logChannel, out var action, out var reason);

                await DeferAsync();

                using (var db = new ApplicationDbContext())
                {
                    var guild = await db
                        .Guilds.Where(guild => guild.Id == Context.Guild.Id)
                        .Include(guild => guild.AuditLogChannels)
                        .SingleOrDefaultAsync();

                    if (guild is null)
                    {
                        await db.Guilds.AddAsync(new() { Id = Context.Guild.Id });
                        await db.SaveChangesAsync();
                        await RespondOrFollowUpAsync(NotConfiguredMessage);
                        return;
                    }

                    var auditLogChannel = guild.AuditLogChannels.FirstOrDefault(channel =>
                        channel.Id == options.LogChannel.Id
                    );

                    if (auditLogChannel is null)
                    {
                        await RespondOrFollowUpAsync(NotConfiguredMessage);
                        return;
                    }

                    if (auditLogChannel.AuditLogType == action)
                    {
                        guild.AuditLogChannels.Remove(auditLogChannel);
                    }
                    else if (auditLogChannel.AuditLogType.HasFlag(action))
                    {
                        auditLogChannel.AuditLogType &= ~action;
                    }
                    else
                    {
                        await RespondOrFollowUpAsync(NotConfiguredMessage);
                        return;
                    }

                    await db.SaveChangesAsync();
                }

                await LogAsync(Context, options.Reason, options.LogChannel.Id);
            }

            public async Task ViewAsync<TChannel, TLogType>(
                string titleSuffix,
                TLogType[] logTypes,
                Func<Guild, ICollection<TChannel>> channelSelector,
                Func<IQueryable<Guild>, IQueryable<Guild>> withInclude,
                Func<TChannel, TLogType> logTypeSelector
            )
                where TLogType : struct, Enum
                where TChannel : Snowflake
            {
                await DeferAsync();

                using var db = new ApplicationDbContext();

                var query = db.Guilds.Where(guild => guild.Id == Context.Guild.Id);

                query = withInclude(query);

                var guild = await query.SingleOrDefaultAsync();

                if (guild is null)
                {
                    await db.Guilds.AddAsync(
                        new() { Id = Context.Guild.Id, Configuration = new() }
                    );
                    await db.SaveChangesAsync();
                    await RespondOrFollowUpAsync(NothingToView);
                    return;
                }

                db.Entry(guild).State = EntityState.Detached;

                var channels = channelSelector(guild);

                if (channels.Count == 0)
                {
                    await RespondOrFollowUpAsync(NothingToView);
                    return;
                }

                var embedDescriptions = channels
                    .SelectMany(channel =>
                    {
                        List<string> descriptions = [];
                        TLogType logType = logTypeSelector(channel);

                        foreach (TLogType log in logTypes)
                        {
                            if (logType.HasFlag(log))
                            {
                                descriptions.Add(
                                    $"{MentionUtils.MentionChannel(channel.Id)} {Format.Bold($"[{log}]")}"
                                );
                            }
                        }

                        return descriptions;
                    })
                    .ToList();

                var embeds = ButtonPaginationManager.GetEmbeds(
                    embedDescriptions,
                    $"{Context.Guild.Name} {titleSuffix}"
                );

                var pagination = new ButtonPaginationBuilder() { Embeds = [.. embeds] };

                await pagination.Build().Init(Context);
            }

            [SlashCommand("view-bot-log", "View the bot log channel configuration")]
            public async Task ViewBotLog() =>
                await ViewAsync(
                    $"Bot Log Channels",
                    BotLogTypes,
                    guild => guild.BotLogChannels,
                    query => query.Include(guild => guild.BotLogChannels),
                    channel => channel.BotLogType
                );

            [SlashCommand("view-audit-log", "View the audit log channel configuration")]
            public async Task ViewAuditLog() =>
                await ViewAsync(
                    $"Audit Log Channels",
                    AuditLogTypes,
                    guild => guild.AuditLogChannels,
                    query => query.Include(guild => guild.AuditLogChannels),
                    channel => channel.AuditLogType
                );

            public readonly record struct LogChannelOptions<T>(
                ITextChannel LogChannel,
                T Action,
                [MaxLength(CaseService.MaxReasonLength)] string? Reason = null
            )
                where T : struct, Enum { }
        }
    }
}
