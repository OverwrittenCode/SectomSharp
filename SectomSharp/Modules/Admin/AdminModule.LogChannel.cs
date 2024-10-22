using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SectomSharp.Data;
using SectomSharp.Data.Enums;
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
            private const string SetOperationDescription =
                "Not providing this value means all variations of the given action will be included";

            [SlashCommand("set-bot-log", "Add or modify a bot log channel configuration")]
            public async Task SetBotLog(
                ITextChannel logChannel,
                BotLogType action,
                [Summary(description: SetOperationDescription)] OperationType? operation = null,
                [MaxLength(CaseService.MaxReasonLength)] string? reason = null
            )
            {
                await DeferAsync();

                using (var db = new ApplicationDbContext())
                {
                    var guild = await db
                        .Guilds.Where(guild => guild.Id == Context.Guild.Id)
                        .Include(guild => guild.BotLogChannels)
                        .SingleOrDefaultAsync();

                    if (guild is null)
                    {
                        guild = (await db.Guilds.AddAsync(new() { Id = Context.Guild.Id })).Entity;
                    }
                    else if (
                        guild.BotLogChannels.Any(channel =>
                            channel.Id == logChannel.Id
                            && channel.BotLogType == action
                            && (channel.OperationType == null || channel.OperationType == operation)
                        )
                    )
                    {
                        await RespondOrFollowUpAsync(AlreadyConfiguredMessage);
                        return;
                    }

                    await db.SaveChangesAsync();
                }

                await LogAsync(Context, reason, logChannel.Id);
            }

            [SlashCommand("set-audit-log", "Add or modify an audit log channel configuration")]
            [RequireBotPermission(ChannelPermission.ManageWebhooks)]
            public async Task SetAuditLog(
                ITextChannel logChannel,
                AuditLogType action,
                [Summary(description: SetOperationDescription)] OperationType? operation = null,
                [MaxLength(CaseService.MaxReasonLength)] string? reason = null
            )
            {
                await DeferAsync();

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

                using (var db = new ApplicationDbContext())
                {
                    var guild = await db
                        .Guilds.Where(guild => guild.Id == Context.Guild.Id)
                        .Include(guild => guild.AuditLogChannels)
                        .SingleOrDefaultAsync();

                    if (guild is null)
                    {
                        guild = (await db.Guilds.AddAsync(new() { Id = Context.Guild.Id })).Entity;
                    }
                    else if (
                        guild.AuditLogChannels.Any(channel =>
                            channel.Id == logChannel.Id
                            && channel.AuditLogType == action
                            && (channel.OperationType == null || channel.OperationType == operation)
                        )
                    )
                    {
                        await RespondOrFollowUpAsync(AlreadyConfiguredMessage);
                        return;
                    }

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
                                    OperationType = operation,
                                }
                            )
                        ).Entity
                    );

                    await db.SaveChangesAsync();
                }

                await LogAsync(Context, reason, logChannel.Id);
            }

            [SlashCommand("remove-bot-log", "Remove a bot log channel configuration")]
            public async Task RemoveBotLog(
                ITextChannel logChannel,
                BotLogType action,
                OperationType? operation = null,
                [MaxLength(CaseService.MaxReasonLength)] string? reason = null
            )
            {
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

                    var match = guild
                        .BotLogChannels.Where(channel =>
                            channel.Id == logChannel.Id
                            && channel.BotLogType == action
                            && channel.OperationType == operation
                        )
                        .SingleOrDefault();

                    if (match is null)
                    {
                        await RespondOrFollowUpAsync(NotConfiguredMessage);
                        return;
                    }

                    guild.BotLogChannels.Remove(match);
                    await db.SaveChangesAsync();
                }

                await LogAsync(Context, reason, logChannel.Id);
            }

            [SlashCommand("remove-audit-log", "Remove an audit log channel configuration")]
            public async Task RemoveAuditLog(
                ITextChannel logChannel,
                AuditLogType action,
                OperationType? operation = null,
                [MaxLength(CaseService.MaxReasonLength)] string? reason = null
            )
            {
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

                    var match = guild
                        .AuditLogChannels.Where(channel =>
                            channel.Id == logChannel.Id
                            && channel.AuditLogType == action
                            && channel.OperationType == operation
                        )
                        .SingleOrDefault();

                    if (match is null)
                    {
                        await RespondOrFollowUpAsync(NotConfiguredMessage);
                        return;
                    }

                    guild.AuditLogChannels.Remove(match);
                    await db.SaveChangesAsync();
                }

                await LogAsync(Context, reason, logChannel.Id);
            }
        }
    }
}
