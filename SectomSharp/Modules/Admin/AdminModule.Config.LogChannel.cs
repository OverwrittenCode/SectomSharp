using System.Data.Common;
using Discord;
using Discord.Interactions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Data.Enums;
using SectomSharp.Extensions;
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
            private readonly ILoggerFactory _loggerFactory;

            /// <inheritdoc />
            public LogChannelModule(ILogger<LogChannelModule> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory, ILoggerFactory loggerFactory) : base(
                logger,
                dbContextFactory
            )
                => _loggerFactory = loggerFactory;

            /// <summary>
            ///     Represents the result of an audit log upsert.
            /// </summary>
            private enum AuditLogUpsertResult
            {
                /// <summary>
                ///     No rows were affected.
                /// </summary>
                NoChange = 0,

                /// <summary>
                ///     The flag has been updated.
                /// </summary>
                [UsedImplicitly] Updated = 1,

                /// <summary>
                ///     An audit log channel has been inserted with a temporary empty string for the webhook url.
                ///     A webhook url is required.
                /// </summary>
                InsertedAndNeedsWebhookUrl = 2
            }

            [SlashCmd("Add or modify a bot log channel configuration")]
            public async Task SetBotLog([ComplexParameter] LogChannelOptions<BotLogType> options)
            {
                options.Deconstruct(out ITextChannel channel, out BotLogType action, out string? reason);

                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                await db.Database.OpenConnectionAsync();
                await using DbCommand cmd = db.Database.GetDbConnection().CreateCommand();

                cmd.CommandText = """
                                  WITH
                                      guild_upsert AS (
                                          INSERT INTO "Guilds" ("Id")
                                          VALUES (@guildId)
                                          ON CONFLICT ("Id") DO NOTHING
                                          RETURNING 1
                                      ),
                                      channel_upsert AS (
                                          INSERT INTO "BotLogChannels" ("Id", "GuildId", "Type")
                                          VALUES (@channelId, @guildId, @action)
                                          ON CONFLICT ("Id") DO UPDATE SET
                                              "Type" = EXCLUDED."Type" | "BotLogChannels"."Type"
                                          WHERE 
                                              EXISTS (SELECT 1 FROM guild_upsert)
                                              OR ("BotLogChannels"."Type" & @action) = 0
                                          RETURNING 1
                                      )
                                  SELECT 1 FROM channel_upsert;
                                  """;

                cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("channelId", channel.Id));
                cmd.Parameters.Add(NpgsqlParameterFactory.FromEnum32("action", action));

                if (await cmd.ExecuteScalarAsync() is null)
                {
                    await RespondOrFollowupAsync(AlreadyConfiguredMessage);
                    return;
                }

                await LogAsync(db, Context, reason);
            }

            [SlashCmd("Add or modify an audit log channel configuration")]
            [RequireBotPermission(GuildPermission.ViewAuditLog)]
            public async Task SetAuditLog([ComplexParameter] LogChannelOptions<AuditLogType> options)
            {
                options.Deconstruct(out ITextChannel channel, out AuditLogType action, out string? reason);

                if (!Context.Guild.CurrentUser.GetPermissions(channel).ManageWebhooks)
                {
                    await RespondOrFollowupAsync(
                        $"Bot requires channel permission {nameof(ChannelPermission.ManageWebhooks)} in {MentionUtils.MentionChannel(channel.Id)}",
                        ephemeral: true
                    );

                    return;
                }

                AuditLogUpsertResult result;

                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                await db.Database.OpenConnectionAsync();
                await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    await using DbCommand cmd = db.Database.GetDbConnection().CreateCommand();
                    cmd.Transaction = transaction.GetDbTransaction();

                    cmd.CommandText = """
                                      WITH
                                          guild_upsert AS (
                                              INSERT INTO "Guilds" ("Id")
                                              VALUES (@guildId)
                                              ON CONFLICT ("Id") DO NOTHING
                                          ),
                                          channel_upsert AS (
                                              INSERT INTO "AuditLogChannels" ("Id", "GuildId", "Type", "WebhookUrl")
                                              VALUES (@channelId, @guildId, @action, '')
                                              ON CONFLICT ("Id") DO UPDATE
                                                    SET "Type" = "AuditLogChannels"."Type" | EXCLUDED."Type"
                                              WHERE ("AuditLogChannels"."Type" & @action) = 0
                                              RETURNING
                                                  CASE
                                                      WHEN xmax = 0 THEN 2
                                                      WHEN "WebhookUrl" <> '' THEN 1
                                                      ELSE 0
                                                  END AS result_tag
                                          )
                                      SELECT * FROM channel_upsert;
                                      """;
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("channelId", channel.Id));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromEnum32("action", action));

                    result = (AuditLogUpsertResult)await cmd.ExecuteScalarAsync<int>();

                    if (result is AuditLogUpsertResult.InsertedAndNeedsWebhookUrl)
                    {
                        IWebhook webhook = (await channel.GetWebhooksAsync()).FirstOrDefault(w => w.Creator.Id == Context.Guild.CurrentUser.Id)
                                        ?? await channel.CreateWebhookAsync(
                                               Context.Guild.CurrentUser.DisplayName,
                                               options: DiscordUtils.GetAuditReasonRequestOptions(Context, "Automated webhook creation")
                                           );

                        string webhookUrl = $"https://discord.com/api/webhooks/{webhook.Id}/{webhook.Token}";
                        await db.AuditLogChannels.Where(x => x.Id == channel.Id).ExecuteUpdateAsync(x => x.SetProperty(c => c.WebhookUrl, webhookUrl));
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await RespondOrFollowupAsync("An error occurred while configuring the audit log. Please try again.", ephemeral: true);
                    throw;
                }

                if (result is AuditLogUpsertResult.NoChange)
                {
                    await RespondOrFollowupAsync(AlreadyConfiguredMessage);
                    return;
                }

                await LogAsync(db, Context, reason, channel.Id);
            }

            [SlashCmd("Remove a bot log channel configuration")]
            public async Task RemoveBotLog([ComplexParameter] LogChannelOptions<BotLogType> options)
            {
                options.Deconstruct(out ITextChannel channel, out BotLogType action, out string? reason);

                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                await db.Database.OpenConnectionAsync();
                await using DbCommand cmd = db.Database.GetDbConnection().CreateCommand();

                cmd.CommandText = """
                                  WITH
                                      deleted AS (
                                          DELETE FROM "BotLogChannels"
                                              WHERE "Id" = @channelId
                                                  AND "GuildId" = @guildId
                                                  AND "Type" = @action
                                              RETURNING 1
                                      ),
                                      updated AS (
                                          UPDATE "BotLogChannels"
                                              SET "Type" = "Type" & ~@action
                                              WHERE
                                                  NOT EXISTS (SELECT 1 FROM deleted)
                                                  AND "Id" = @channelId
                                                  AND "GuildId" = @guildId
                                                  AND "Type" & @action <> 0
                                              RETURNING 1
                                      )
                                  SELECT EXISTS (
                                      SELECT 1 FROM deleted
                                      UNION ALL
                                      SELECT 1 FROM updated
                                  );
                                  """;

                cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("channelId", channel.Id));
                cmd.Parameters.Add(NpgsqlParameterFactory.FromEnum32("action", action));

                if (await cmd.ExecuteScalarAsync() is not true)
                {
                    await RespondOrFollowupAsync(NotConfiguredMessage);
                    return;
                }

                await LogAsync(db, Context, reason, channel.Id);
            }

            [SlashCmd("Remove an audit log channel configuration")]
            public async Task RemoveAuditLog([ComplexParameter] LogChannelOptions<AuditLogType> options)
            {
                options.Deconstruct(out ITextChannel channel, out AuditLogType action, out string? reason);

                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                await db.Database.OpenConnectionAsync();
                await using DbCommand cmd = db.Database.GetDbConnection().CreateCommand();

                cmd.CommandText = """
                                  WITH
                                      deleted AS (
                                          DELETE FROM "BotLogChannels"
                                              WHERE "Id" = @channelId
                                                  AND "GuildId" = @guildId
                                                  AND "Type" = @action
                                              RETURNING 1
                                      ),
                                      updated AS (
                                          UPDATE "BotLogChannels"
                                              SET "Type" = "Type" & ~@action
                                              WHERE
                                                  NOT EXISTS (SELECT 1 FROM deleted)
                                                  AND "Id" = @channelId
                                                  AND "GuildId" = @guildId
                                                  AND "Type" & @action <> 0
                                              RETURNING 1
                                      )
                                  SELECT EXISTS (
                                      SELECT 1 FROM deleted
                                      UNION ALL
                                      SELECT 1 FROM updated
                                  );
                                  """;

                cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("channelId", channel.Id));
                cmd.Parameters.Add(NpgsqlParameterFactory.FromEnum32("action", action));

                if (await cmd.ExecuteScalarAsync() is not true)
                {
                    await RespondOrFollowupAsync(NotConfiguredMessage);
                    return;
                }

                await LogAsync(db, Context, reason, channel.Id);
            }

            [SlashCmd("View the bot log channel configuration")]
            public async Task ViewBotLog()
            {
                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                List<string> embedDescriptions = await db.BotLogChannels.Where(channel => channel.GuildId == Context.Guild.Id)
                                                         .Select(channel => new
                                                              {
                                                                  channel.Id,
                                                                  channel.Type
                                                              }
                                                          )
                                                         .AsAsyncEnumerable()
                                                         .SelectManyAwait(result => ValueTask.FromResult(
                                                                  BotLogTypes.Where(flag => result.Type.HasFlag(flag))
                                                                             .Select(log => $"{MentionUtils.MentionChannel(result.Id)} {Format.Bold($"[{log}]")}")
                                                                             .ToAsyncEnumerable()
                                                              )
                                                          )
                                                         .ToListAsync();

                Embed[] embeds = ButtonPaginationManager.GetEmbeds(embedDescriptions, $"{Context.Guild.Name} Bot Log Channels");

                if (embeds.Length == 0)
                {
                    await RespondOrFollowupAsync(NothingToView);
                    return;
                }

                await new ButtonPaginationManager(_loggerFactory, Context) { Embeds = embeds }.InitAsync(Context);
            }

            [SlashCmd("View the audit log channel configuration")]
            public async Task ViewAuditLog()
            {
                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                List<string> embedDescriptions = await db.AuditLogChannels.Where(channel => channel.GuildId == Context.Guild.Id)
                                                         .Select(channel => new
                                                              {
                                                                  channel.Id,
                                                                  channel.Type
                                                              }
                                                          )
                                                         .AsAsyncEnumerable()
                                                         .SelectManyAwait(result => ValueTask.FromResult(
                                                                  AuditLogTypes.Where(flag => result.Type.HasFlag(flag))
                                                                               .Select(log => $"{MentionUtils.MentionChannel(result.Id)} {Format.Bold($"[{log}]")}")
                                                                               .ToAsyncEnumerable()
                                                              )
                                                          )
                                                         .ToListAsync();

                Embed[] embeds = ButtonPaginationManager.GetEmbeds(embedDescriptions, $"{Context.Guild.Name} Audit Log Channels");

                if (embeds.Length == 0)
                {
                    await RespondOrFollowupAsync(NothingToView);
                    return;
                }

                await new ButtonPaginationManager(_loggerFactory, Context) { Embeds = embeds }.InitAsync(Context);
            }

            public readonly record struct LogChannelOptions<T>(ITextChannel Channel, T Action, [ReasonMaxLength] string? Reason = null)
                where T : struct, Enum;
        }
    }
}
