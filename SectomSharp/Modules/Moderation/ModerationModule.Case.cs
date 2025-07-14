using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Data.CompositeTypes;
using SectomSharp.Data.Entities;
using SectomSharp.Data.Enums;
using SectomSharp.Extensions;
using SectomSharp.Managers.Pagination.Button;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Moderation;

public sealed partial class ModerationModule
{
    [Group("case", "Container of all cases in the server")]
    [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
    public sealed class CaseModule : BaseModule<CaseModule>
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <inheritdoc />
        public CaseModule(ILogger<CaseModule> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory, ILoggerFactory loggerFactory) : base(logger, dbContextFactory)
            => _loggerFactory = loggerFactory;

        [SlashCmd("View a specific case on the server")]
        public async Task View([MinLength(CaseConfiguration.IdLength)] [MaxLength(CaseConfiguration.IdLength)] string id)
        {
            await DeferAsync();
            (string? LogMessageUrl, EmbedBuilder EmbedBuilder)? result = null;
            Stopwatch stopwatch;
            await using (ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync())
            {
                await db.Database.OpenConnectionAsync();
                await using DbCommand cmd = db.Database.GetDbConnection().CreateCommand();

                cmd.CommandText = $"""
                                   SELECT
                                       c."LogMessageUrl",
                                       c."CreatedAt",
                                       c."Fields",
                                       c."Color",
                                       c."Description",
                                       c."PerpetratorAvatarUrl",
                                       CONCAT(
                                         CASE c."LogType"
                                             WHEN 1    THEN '{nameof(BotLogType.Warn)}'
                                             WHEN 2    THEN '{nameof(BotLogType.Ban)}'
                                             WHEN 4    THEN '{nameof(BotLogType.Softban)}'
                                             WHEN 8    THEN '{nameof(BotLogType.Timeout)}'
                                             WHEN 16   THEN '{nameof(BotLogType.Configuration)}'
                                             WHEN 32   THEN '{nameof(BotLogType.Kick)}'
                                             WHEN 64   THEN '{nameof(BotLogType.Deafen)}'
                                             WHEN 128  THEN '{nameof(BotLogType.Mute)}'
                                             WHEN 256  THEN '{nameof(BotLogType.Nick)}'
                                             WHEN 512  THEN '{nameof(BotLogType.Purge)}'
                                             WHEN 1024 THEN '{nameof(BotLogType.ModNote)}'
                                             ELSE 'Unknown'
                                         END,
                                         CASE "OperationType"
                                             WHEN 0 THEN '{nameof(OperationType.Create)}'
                                             WHEN 1 THEN '{nameof(OperationType.Update)}'
                                             WHEN 2 THEN '{nameof(OperationType.Delete)}'
                                             ELSE 'Unknown'
                                         END,
                                         ' | ',
                                         "Id"
                                       ) AS "Header",
                                       CONCAT('Perpetrator: ', "PerpetratorId") AS "Footer"
                                   FROM "Cases" c
                                   WHERE c."GuildId" = @guildId AND c."Id" = @id
                                   """;
                cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                cmd.Parameters.Add(NpgsqlParameterFactory.FromVarchar("id", id));

                stopwatch = Stopwatch.StartNew();
                await using (DbDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleRow | CommandBehavior.CloseConnection))
                {
                    if (await reader.ReadAsync())
                    {
                        string? logMessageUrl = reader.IsDBNull(0) ? null : reader.GetString(0);
                        DateTime createdAt = reader.GetDateTime(1);
                        List<EmbedFieldBuilder> fields = CompositeEmbedField.ToBuilders(reader.GetFieldValue<CompositeEmbedField[]>(2));
                        var color = new Color((uint)reader.GetInt32(3));
                        string description = reader.GetString(4);
                        string avatarUrl = reader.GetString(5);
                        string header = reader.GetString(6);
                        string footer = reader.GetString(7);

                        var embedBuilder = new EmbedBuilder
                        {
                            Timestamp = createdAt,
                            Fields = fields,
                            Color = color,
                            Description = description,
                            ThumbnailUrl = avatarUrl,
                            Author = new EmbedAuthorBuilder { Name = header },
                            Footer = new EmbedFooterBuilder { Text = footer }
                        };

                        result = (logMessageUrl, embedBuilder);
                    }

                    stopwatch.Stop();
                }
            }

            Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);

            if (!result.HasValue)
            {
                await FollowupAsync("Invalid case id provided.");
                return;
            }

            await FollowupAsync(embeds: [result.Value.EmbedBuilder.Build()], components: CaseUtils.GenerateLogMessageButton(result.Value.LogMessageUrl));
        }

        [SlashCmd("List and filter all cases on the server")]
        public async Task List(IUser? target = null, IUser? perpetrator = null, IChannel? channel = null, BotLogType? logType = null, OperationType? operationType = null)
        {
            await DeferAsync();

            Embed[] embeds = [];

            Stopwatch stopwatch;
            await using (ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync())
            {
                await db.Database.OpenConnectionAsync();
                await using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = $"""
                                       WITH log_type_names AS (
                                           SELECT * FROM (VALUES
                                               (1,    '{nameof(BotLogType.Warn)}'),
                                               (2,    '{nameof(BotLogType.Ban)}'),
                                               (4,    '{nameof(BotLogType.Softban)}'),
                                               (8,    '{nameof(BotLogType.Timeout)}'),
                                               (16,   '{nameof(BotLogType.Configuration)}'),
                                               (32,   '{nameof(BotLogType.Kick)}'),
                                               (64,   '{nameof(BotLogType.Deafen)}'),
                                               (128,  '{nameof(BotLogType.Mute)}'),
                                               (256,  '{nameof(BotLogType.Nick)}'),
                                               (512,  '{nameof(BotLogType.Purge)}'),
                                               (1024, '{nameof(BotLogType.ModNote)}')
                                           ) AS l(type, name)
                                       ),
                                       operation_type_names AS (
                                           SELECT * FROM (VALUES
                                               (0, '{nameof(OperationType.Create)}'),
                                               (1, '{nameof(OperationType.Update)}'),
                                               (2, '{nameof(OperationType.Delete)}')
                                           ) AS o(type, name)
                                       ),
                                       filtered_cases AS (
                                           SELECT
                                               c."Id",
                                               c."LogType",
                                               c."OperationType",
                                               c."CreatedAt",
                                               row_number() OVER (ORDER BY c."CreatedAt" DESC) AS rn
                                           FROM "Cases" c
                                           WHERE c."GuildId" = @guildId
                                             AND (@targetId IS NULL OR c."TargetId" = @targetId)
                                             AND (@perpetratorId IS NULL OR c."PerpetratorId" = @perpetratorId)
                                             AND (@channelId IS NULL OR c."ChannelId" = @channelId)
                                             AND (@logType IS NULL OR c."LogType" = @logType)
                                             AND (@operationType IS NULL OR c."OperationType" = @operationType)
                                           ORDER BY c."CreatedAt" DESC
                                           LIMIT 1000
                                       ),
                                       batched AS (
                                           SELECT
                                               '`' || fc."Id" || '` **[' ||
                                               COALESCE(lt.name, fc."LogType"::text) ||
                                               COALESCE(ot.name, fc."OperationType"::text) || ']** ' || '<t:' ||
                                               floor(extract(epoch from fc."CreatedAt" AT TIME ZONE 'UTC'))::bigint || ':R>' AS entry,
                                               (fc.rn - 1) / @batchSize AS batch_index,
                                               fc.rn
                                           FROM filtered_cases fc
                                           LEFT JOIN log_type_names lt ON fc."LogType" = lt.type
                                           LEFT JOIN operation_type_names ot ON fc."OperationType" = ot.type
                                       )
                                       SELECT
                                           string_agg(b.entry, E'\n') AS "Batch",
                                           count(*) OVER () AS "TotalPages"
                                       FROM batched b
                                       GROUP BY b.batch_index
                                       ORDER BY MIN(b.rn)
                                       LIMIT 100;
                                       """;

                    cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("targetId", target?.Id));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("perpetratorId", perpetrator?.Id));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("channelId", channel?.Id));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromEnum32("logType", logType));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromEnum32("operationType", operationType));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromInt32("batchSize", ButtonPaginationManager.ChunkSize));

                    int page = 0;
                    var embedBuilder = new EmbedBuilder
                    {
                        Title = $"{Context.Guild.Name} Cases",
                        Color = Storage.LightGold
                    };
                    stopwatch = Stopwatch.StartNew();
                    await using (DbDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.CloseConnection))
                    {
                        if (await reader.ReadAsync())
                        {
                            string batch = reader.GetString(0);
                            int totalPages = reader.GetInt32(1);

                            embeds = new Embed[totalPages];

                            embedBuilder.Description = batch;
                            if (totalPages > 1)
                            {
                                embedBuilder.Footer = new EmbedFooterBuilder { Text = $"Page 1/{totalPages}" };
                            }

                            embeds[page++] = embedBuilder.Build();

                            while (await reader.ReadAsync())
                            {
                                batch = reader.GetString(0);

                                embedBuilder.Description = batch;
                                embedBuilder.Footer = new EmbedFooterBuilder { Text = $"Page {page + 1}/{totalPages}" };
                                embeds[page++] = embedBuilder.Build();
                            }
                        }

                        stopwatch.Stop();
                    }
                }
            }

            Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);

            switch (embeds.Length)
            {
                case 0:
                    await FollowupAsync(NothingToView, ephemeral: true);
                    return;
                case 1:
                    await FollowupAsync(embeds: embeds);
                    return;
                default:
                    await new ButtonPaginationManager(_loggerFactory, Context) { Embeds = embeds }.InitAsync(Context);
                    return;
            }
        }
    }
}
