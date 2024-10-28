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
                options.Deconstruct(
                    out ITextChannel channel,
                    out BotLogType action,
                    out var reason
                );

                await DeferAsync();

                await using (var db = new ApplicationDbContext())
                {
                    Guild? guild = await db
                        .Guilds.Where(guild => guild.Id == Context.Guild.Id)
                        .Include(guild => guild.BotLogChannels)
                        .SingleOrDefaultAsync();

                    guild ??= (await db.Guilds.AddAsync(new()
                    {
                        Id = Context.Guild.Id
                    })).Entity;

                    BotLogChannel? botLogChannel = guild.BotLogChannels.FirstOrDefault(botLogChannel =>
                        botLogChannel.Id == channel.Id
                    );

                    if (botLogChannel is null)
                    {
                        guild.BotLogChannels.Add(
                            new()
                            {
                                Id = channel.Id, GuildId = Context.Guild.Id, Type = action
                            }
                        );
                    }
                    else if (!botLogChannel.Type.HasFlag(action))
                    {
                        botLogChannel.Type |= action;
                    }
                    else
                    {
                        await RespondOrFollowUpAsync(AlreadyConfiguredMessage);
                        return;
                    }

                    await db.SaveChangesAsync();
                }

                await LogAsync(Context, reason, channel.Id);
            }

            [SlashCommand("set-audit-log", "Add or modify an audit log channel configuration")]
            [RequireBotPermission(GuildPermission.ViewAuditLog)]
            public async Task SetAuditLog(
                [ComplexParameter] LogChannelOptions<AuditLogType> options
            )
            {
                options.Deconstruct(
                    out ITextChannel? channel,
                    out AuditLogType action,
                    out var reason
                );

                if (!Context.Guild.CurrentUser.GetPermissions(channel).ManageWebhooks)
                {
                    await RespondOrFollowUpAsync(
                        $"Bot requires channel permission {ChannelPermission.ManageWebhooks} in {MentionUtils.MentionChannel(channel.Id)}",
                        ephemeral: true
                    );
                }

                await DeferAsync();

                await using (var db = new ApplicationDbContext())
                {
                    Guild? guild = await db
                        .Guilds.Where(guild => guild.Id == Context.Guild.Id)
                        .Include(guild => guild.AuditLogChannels)
                        .SingleOrDefaultAsync();

                    guild ??= (await db.Guilds.AddAsync(new()
                    {
                        Id = Context.Guild.Id
                    })).Entity;

                    AuditLogChannel? auditLogChannel = guild.AuditLogChannels.SingleOrDefault(
                        auditLogChannel => auditLogChannel.Id == channel.Id
                    );

                    if (auditLogChannel is null)
                    {
                        IWebhook webhook =
                            (await channel.GetWebhooksAsync()).FirstOrDefault(webhook =>
                                webhook.Creator.Id == Context.Guild.CurrentUser.Id
                            )
                            ?? await channel.CreateWebhookAsync(
                                Context.Guild.CurrentUser.DisplayName,
                                options: DiscordUtils.GetAuditReasonRequestOptions(
                                    Context,
                                    "Automated request for audit logging."
                                )
                            );

                        guild.AuditLogChannels.Add(
                            (
                                await db.AuditLogChannels.AddAsync(
                                    new()
                                    {
                                        Id = channel.Id,
                                        GuildId = Context.Guild.Id,
                                        WebhookUrl =
                                            $"https://discord.com/api/webhooks/{webhook.Id}/{webhook.Token}",
                                        Type = action
                                    }
                                )
                            ).Entity
                        );
                    }
                    else if (!auditLogChannel.Type.HasFlag(action))
                    {
                        auditLogChannel.Type |= action;
                    }
                    else
                    {
                        await RespondOrFollowUpAsync(AlreadyConfiguredMessage);
                        return;
                    }

                    await db.SaveChangesAsync();
                }

                await LogAsync(Context, reason, channel.Id);
            }

            [SlashCommand("remove-bot-log", "Remove a bot log channel configuration")]
            public async Task RemoveBotLog([ComplexParameter] LogChannelOptions<BotLogType> options)
            {
                options.Deconstruct(
                    out ITextChannel? channel,
                    out BotLogType action,
                    out var reason
                );

                await DeferAsync();

                await using (var db = new ApplicationDbContext())
                {
                    Guild? guild = await db
                        .Guilds.Where(guild => guild.Id == Context.Guild.Id)
                        .Include(guild => guild.BotLogChannels)
                        .SingleOrDefaultAsync();

                    if (guild is null)
                    {
                        await db.Guilds.AddAsync(new()
                        {
                            Id = Context.Guild.Id
                        });
                        await db.SaveChangesAsync();
                        await RespondOrFollowUpAsync(NotConfiguredMessage);
                        return;
                    }

                    BotLogChannel? botLogChannel = guild.BotLogChannels.FirstOrDefault(botLogChannel =>
                        botLogChannel.Id == channel.Id
                    );

                    if (botLogChannel is null)
                    {
                        await RespondOrFollowUpAsync(NotConfiguredMessage);
                        return;
                    }

                    if (botLogChannel.Type == action)
                    {
                        guild.BotLogChannels.Remove(botLogChannel);
                    }
                    else if (botLogChannel.Type.HasFlag(action))
                    {
                        botLogChannel.Type &= ~action;
                    }
                    else
                    {
                        await RespondOrFollowUpAsync(NotConfiguredMessage);
                        return;
                    }

                    await db.SaveChangesAsync();
                }

                await LogAsync(Context, reason, options.Channel.Id);
            }

            [SlashCommand("remove-audit-log", "Remove an audit log channel configuration")]
            public async Task RemoveAuditLog(
                [ComplexParameter] LogChannelOptions<AuditLogType> options
            )
            {
                options.Deconstruct(
                    out ITextChannel? channel,
                    out AuditLogType action,
                    out var reason
                );

                await DeferAsync();

                await using (var db = new ApplicationDbContext())
                {
                    Guild? guild = await db
                        .Guilds.Where(guild => guild.Id == Context.Guild.Id)
                        .Include(guild => guild.AuditLogChannels)
                        .SingleOrDefaultAsync();

                    if (guild is null)
                    {
                        await db.Guilds.AddAsync(new()
                        {
                            Id = Context.Guild.Id
                        });
                        await db.SaveChangesAsync();
                        await RespondOrFollowUpAsync(NotConfiguredMessage);
                        return;
                    }

                    AuditLogChannel? auditLogChannel = guild.AuditLogChannels.FirstOrDefault(
                        auditLogChannel => auditLogChannel.Id == channel.Id
                    );

                    if (auditLogChannel is null)
                    {
                        await RespondOrFollowUpAsync(NotConfiguredMessage);
                        return;
                    }

                    if (auditLogChannel.Type == action)
                    {
                        guild.AuditLogChannels.Remove(auditLogChannel);
                    }
                    else if (auditLogChannel.Type.HasFlag(action))
                    {
                        auditLogChannel.Type &= ~action;
                    }
                    else
                    {
                        await RespondOrFollowUpAsync(NotConfiguredMessage);
                        return;
                    }

                    await db.SaveChangesAsync();
                }


                await LogAsync(Context, reason, channel.Id);
            }

            [SlashCommand("view-bot-log", "View the bot log channel configuration")]
            public async Task ViewBotLog() =>
                await ViewAsync(
                    "Bot Log Channels",
                    BotLogTypes,
                    guild => guild.BotLogChannels,
                    query => query.Include(guild => guild.BotLogChannels),
                    channel => channel.Type
                );

            [SlashCommand("view-audit-log", "View the audit log channel configuration")]
            public async Task ViewAuditLog() =>
                await ViewAsync(
                    "Audit Log Channels",
                    AuditLogTypes,
                    guild => guild.AuditLogChannels,
                    query => query.Include(guild => guild.AuditLogChannels),
                    channel => channel.Type
                );

            private async Task ViewAsync<TChannel, TLogType>(
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

                await using var db = new ApplicationDbContext();

                IQueryable<Guild> query = db.Guilds.Where(guild => guild.Id == Context.Guild.Id);

                query = withInclude(query);

                Guild? guild = await query.SingleOrDefaultAsync();

                if (guild is null)
                {
                    await db.Guilds.AddAsync(
                        new()
                        {
                            Id = Context.Guild.Id, Configuration = new()
                        }
                    );
                    await db.SaveChangesAsync();
                    await RespondOrFollowUpAsync(NothingToView);
                    return;
                }

                db.Entry(guild).State = EntityState.Detached;

                ICollection<TChannel> channels = channelSelector(guild);

                if (channels.Count == 0)
                {
                    await RespondOrFollowUpAsync(NothingToView);
                    return;
                }

                List<string> embedDescriptions = channels
                    .SelectMany(channel =>
                    {
                        List<string> descriptions = [];
                        TLogType logType = logTypeSelector(channel);

                        descriptions.AddRange(
                            from log in logTypes
                            where logType.HasFlag(log)
                            select $"{MentionUtils.MentionChannel(channel.Id)} {Format.Bold($"[{log}]")}"
                        );

                        return descriptions;
                    })
                    .ToList();

                Embed[] embeds = ButtonPaginationManager.GetEmbeds(
                    embedDescriptions,
                    $"{Context.Guild.Name} {titleSuffix}"
                );

                var pagination = new ButtonPaginationBuilder
                {
                    Embeds = [.. embeds]
                };

                await pagination.Build().Init(Context);
            }

            public readonly record struct LogChannelOptions<T>(
                ITextChannel Channel,
                T Action,
                [MaxLength(CaseService.MaxReasonLength)] string? Reason = null
            )
                where T : struct, Enum;
        }
    }
}
