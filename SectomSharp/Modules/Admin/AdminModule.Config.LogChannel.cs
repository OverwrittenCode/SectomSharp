using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Data.Entities;
using SectomSharp.Data.Enums;
using SectomSharp.Managers.Pagination.Builders;
using SectomSharp.Managers.Pagination.Button;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Admin;

public sealed partial class AdminModule
{
    public sealed partial class ConfigModule
    {
        [Group("log-channel", "Log Channel configuration")]
        public sealed class LogChannelModule : BaseModule<LogChannelModule>
        {
            private static readonly BotLogType[] BotLogTypes = Enum.GetValues<BotLogType>();
            private static readonly AuditLogType[] AuditLogTypes = Enum.GetValues<AuditLogType>();

            /// <inheritdoc />
            public LogChannelModule(ILogger<LogChannelModule> logger) : base(logger) { }

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
                        new Guild
                        {
                            Id = Context.Guild.Id,
                            Configuration = new Configuration()
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

                List<string> embedDescriptions = channels.SelectMany(channel =>
                                                              {
                                                                  List<string> descriptions = [];
                                                                  TLogType logType = logTypeSelector(channel);

                                                                  descriptions.AddRange(
                                                                      from log in logTypes
                                                                      where logType.HasFlag(log)
                                                                      select $"{MentionUtils.MentionChannel(channel.Id)} {Format.Bold($"[{log}]")}"
                                                                  );

                                                                  return descriptions;
                                                              }
                                                          )
                                                         .ToList();

                Embed[] embeds = ButtonPaginationManager.GetEmbeds(embedDescriptions, $"{Context.Guild.Name} {titleSuffix}");
                var pagination = new ButtonPaginationBuilder
                {
                    Embeds = [.. embeds]
                };
                await pagination.Build().Init(Context);
            }

            [SlashCmd("Add or modify a bot log channel configuration")]
            public async Task SetBotLog([ComplexParameter] LogChannelOptions<BotLogType> options)
            {
                await DeferAsync();

                options.Deconstruct(out ITextChannel channel, out BotLogType action, out string? reason);

                await using (var db = new ApplicationDbContext())
                {
                    Guild guild = await db.Guilds.Where(guild => guild.Id == Context.Guild.Id).Include(guild => guild.BotLogChannels).SingleOrDefaultAsync()
                               ?? (await db.Guilds.AddAsync(
                                      new Guild
                                      {
                                          Id = Context.Guild.Id
                                      }
                                  )).Entity;

                    BotLogChannel? botLogChannel = guild.BotLogChannels.FirstOrDefault(botLogChannel => botLogChannel.Id == channel.Id);

                    if (botLogChannel is null)
                    {
                        guild.BotLogChannels.Add(
                            new BotLogChannel
                            {
                                Id = channel.Id,
                                GuildId = Context.Guild.Id,
                                Type = action
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

            [SlashCmd("Add or modify an audit log channel configuration")]
            [RequireBotPermission(GuildPermission.ViewAuditLog)]
            public async Task SetAuditLog([ComplexParameter] LogChannelOptions<AuditLogType> options)
            {
                options.Deconstruct(out ITextChannel channel, out AuditLogType action, out string? reason);

                if (!Context.Guild.CurrentUser.GetPermissions(channel).ManageWebhooks)
                {
                    await RespondOrFollowUpAsync(
                        $"Bot requires channel permission {nameof(ChannelPermission.ManageWebhooks)} in {MentionUtils.MentionChannel(channel.Id)}",
                        ephemeral: true
                    );
                }

                await DeferAsync();

                await using (var db = new ApplicationDbContext())
                {
                    Guild guild = await db.Guilds.Where(guild => guild.Id == Context.Guild.Id).Include(guild => guild.AuditLogChannels).SingleOrDefaultAsync()
                               ?? (await db.Guilds.AddAsync(
                                      new Guild
                                      {
                                          Id = Context.Guild.Id
                                      }
                                  )).Entity;

                    AuditLogChannel? auditLogChannel = guild.AuditLogChannels.SingleOrDefault(auditLogChannel => auditLogChannel.Id == channel.Id);

                    if (auditLogChannel is null)
                    {
                        IWebhook webhook = (await channel.GetWebhooksAsync()).FirstOrDefault(webhook => webhook.Creator.Id == Context.Guild.CurrentUser.Id)
                                        ?? await channel.CreateWebhookAsync(
                                               Context.Guild.CurrentUser.DisplayName,
                                               options: DiscordUtils.GetAuditReasonRequestOptions(Context, "Automated request for audit logging.")
                                           );

                        var logChannel = new AuditLogChannel
                        {
                            Id = channel.Id,
                            GuildId = Context.Guild.Id,
                            WebhookUrl = $"https://discord.com/api/webhooks/{webhook.Id}/{webhook.Token}",
                            Type = action
                        };

                        guild.AuditLogChannels.Add(logChannel);
                        await db.AuditLogChannels.AddAsync(logChannel);
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

            [SlashCmd("Remove a bot log channel configuration")]
            public async Task RemoveBotLog([ComplexParameter] LogChannelOptions<BotLogType> options)
            {
                options.Deconstruct(out ITextChannel channel, out BotLogType action, out string? reason);

                await DeferAsync();

                await using (var db = new ApplicationDbContext())
                {
                    Guild? guild = await db.Guilds.Where(guild => guild.Id == Context.Guild.Id).Include(guild => guild.BotLogChannels).SingleOrDefaultAsync();

                    if (guild is null)
                    {
                        await db.Guilds.AddAsync(
                            new Guild
                            {
                                Id = Context.Guild.Id
                            }
                        );

                        await db.SaveChangesAsync();
                        await RespondOrFollowUpAsync(NotConfiguredMessage);
                        return;
                    }

                    BotLogChannel? botLogChannel = guild.BotLogChannels.FirstOrDefault(botLogChannel => botLogChannel.Id == channel.Id);

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

            [SlashCmd("Remove an audit log channel configuration")]
            public async Task RemoveAuditLog([ComplexParameter] LogChannelOptions<AuditLogType> options)
            {
                options.Deconstruct(out ITextChannel channel, out AuditLogType action, out string? reason);

                await DeferAsync();

                await using (var db = new ApplicationDbContext())
                {
                    Guild? guild = await db.Guilds.Where(guild => guild.Id == Context.Guild.Id).Include(guild => guild.AuditLogChannels).SingleOrDefaultAsync();

                    if (guild is null)
                    {
                        await db.Guilds.AddAsync(
                            new Guild
                            {
                                Id = Context.Guild.Id
                            }
                        );

                        await db.SaveChangesAsync();
                        await RespondOrFollowUpAsync(NotConfiguredMessage);
                        return;
                    }

                    AuditLogChannel? auditLogChannel = guild.AuditLogChannels.FirstOrDefault(auditLogChannel => auditLogChannel.Id == channel.Id);

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

            [SlashCmd("View the bot log channel configuration")]
            public async Task ViewBotLog()
                => await ViewAsync("Bot Log Channels", BotLogTypes, guild => guild.BotLogChannels, query => query.Include(guild => guild.BotLogChannels), channel => channel.Type);

            [SlashCmd("View the audit log channel configuration")]
            public async Task ViewAuditLog()
                => await ViewAsync(
                    "Audit Log Channels",
                    AuditLogTypes,
                    guild => guild.AuditLogChannels,
                    query => query.Include(guild => guild.AuditLogChannels),
                    channel => channel.Type
                );

            public readonly record struct LogChannelOptions<T>(ITextChannel Channel, T Action, [ReasonMaxLength] string? Reason = null)
                where T : struct, Enum;
        }
    }
}
