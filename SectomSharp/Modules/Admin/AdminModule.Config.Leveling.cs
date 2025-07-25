using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Data.Entities;
using SectomSharp.Extensions;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Admin;

public sealed partial class AdminModule
{
    public sealed partial class ConfigModule
    {
        [Group("leveling", "Leveling configuration")]
        public sealed class LevelingModule : DisableableModule<LevelingModule, LevelingConfiguration>
        {
            /// <inheritdoc />
            public LevelingModule(ILogger<BaseModule<LevelingModule>> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(logger, dbContextFactory) { }

            private async Task ModifySettingsAsync(string? reason, bool? accumulateMultipliers = null, double? globalMultiplier = null, uint? globalCooldown = null)
            {
                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                await db.Database.OpenConnectionAsync();
                object? scalarResult;
                Stopwatch stopwatch;
                await using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = """
                                      UPDATE "Guilds"
                                      SET
                                          "Configuration_Leveling_AccumulateMultipliers" = COALESCE(@accumulateMultipliers, "Configuration_Leveling_AccumulateMultipliers"),
                                          "Configuration_Leveling_GlobalMultiplier" = COALESCE(@globalMultiplier, "Configuration_Leveling_GlobalMultiplier"),
                                          "Configuration_Leveling_GlobalCooldown" = COALESCE(@globalCooldown, "Configuration_Leveling_GlobalCooldown")
                                      WHERE "Id" = @guildId
                                      AND (
                                          (@accumulateMultipliers IS NOT NULL AND @accumulateMultipliers != "Configuration_Leveling_AccumulateMultipliers")
                                          OR (@globalMultiplier IS NOT NULL AND @globalMultiplier != "Configuration_Leveling_GlobalMultiplier")
                                          OR (@globalCooldown IS NOT NULL AND @globalCooldown != "Configuration_Leveling_GlobalCooldown")
                                      )
                                      RETURNING 1
                                      """;

                    cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromBoolean("accumulateMultipliers", accumulateMultipliers));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromDouble("globalMultiplier", globalMultiplier));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromNonNegativeInt32("globalCooldown", globalCooldown));

                    stopwatch = Stopwatch.StartNew();
                    scalarResult = await cmd.ExecuteScalarAsync();
                    stopwatch.Stop();
                }

                Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);
                if (scalarResult is null)
                {
                    await FollowupAsync(AlreadyConfiguredMessage);
                    return;
                }

                await LogAsync(db, Context, reason);
            }

            /// <inheritdoc />
            protected override async Task SetIsDisabledAsync(bool isDisabled, string? reason)
            {
                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                await db.Database.OpenConnectionAsync();
                object? scalarResult;
                Stopwatch stopwatch;
                await using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = """
                                      INSERT INTO "Guilds" ("Id", "Configuration_Leveling_IsDisabled")
                                      VALUES (@guildId, @isDisabled)
                                      ON CONFLICT ("Id") DO UPDATE
                                          SET "Configuration_Leveling_IsDisabled" = @isDisabled
                                          WHERE "Guilds"."Configuration_Leveling_IsDisabled" IS DISTINCT FROM @isDisabled
                                      RETURNING 1
                                      """;

                    cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromBoolean("isDisabled", isDisabled));

                    stopwatch = Stopwatch.StartNew();
                    scalarResult = await cmd.ExecuteScalarAsync();
                    stopwatch.Stop();
                }

                Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);
                if (scalarResult is null)
                {
                    await FollowupAsync(AlreadyConfiguredMessage);
                    return;
                }

                await LogAsync(db, Context, reason);
            }

            [SlashCmd("Set if multipliers should accumulate")]
            public async Task SetAccumulateMultipliers(bool accumulateMultipliers, [ReasonMaxLength] string? reason = null)
                => await ModifySettingsAsync(reason, accumulateMultipliers);

            [SlashCmd("Set the global multiplier")]
            public async Task SetGlobalMultiplier([MinValue(1)] [MaxValue(10)] double globalMultiplier, [ReasonMaxLength] string? reason = null)
                => await ModifySettingsAsync(reason, globalMultiplier: globalMultiplier);

            [SlashCmd("Set the global cooldown in seconds")]
            public async Task SetGlobalCooldown([MinValue(1)] uint globalCooldown, [ReasonMaxLength] string? reason = null)
                => await ModifySettingsAsync(reason, globalCooldown: globalCooldown);

            [SlashCmd("Adds an auto role on reaching a certain level")]
            public async Task AddAutoRole(
                SocketRole role,
                [Summary(description: "The level that grants them the role")] [MinValue(1)] uint level,
                [Summary(description: "The overriden multiplier")] [MinValue(1)] double? multiplier = null,
                [Summary(description: "The overriden cooldown in seconds")] [MinValue(1)] [MaxValue(60)] uint? cooldown = null,
                [ReasonMaxLength] string? reason = null
            )
            {
                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                await db.Database.OpenConnectionAsync();
                object? scalarResult;
                Stopwatch stopwatch;
                await using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = """
                                      WITH
                                          guild_upsert AS (
                                              INSERT INTO "Guilds" ("Id")
                                              VALUES (@guildId)
                                              ON CONFLICT ("Id") DO NOTHING
                                          ),
                                          inserted AS (
                                              INSERT INTO "LevelingRoles" ("Id", "GuildId", "Level", "Multiplier", "Cooldown")
                                              VALUES (@roleId, @guildId, @level, @multiplier, @cooldown)
                                              ON CONFLICT DO NOTHING
                                              RETURNING 1
                                          )
                                          SELECT 1 FROM inserted;
                                      """;

                    cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("roleId", role.Id));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromNonNegativeInt32("level", level));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromDouble("multiplier", multiplier));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromNonNegativeInt32("cooldown", cooldown));

                    stopwatch = Stopwatch.StartNew();
                    scalarResult = await cmd.ExecuteScalarAsync();
                    stopwatch.Stop();
                }

                Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);
                if (scalarResult is null)
                {
                    await FollowupAsync(AlreadyConfiguredMessage);
                    return;
                }

                await LogAsync(db, Context, reason);
            }

            [SlashCmd("Removes an auto role for a certain level")]
            public async Task RemoveAutoRole([Summary(description: "The level to remove")] [MinValue(1)] uint level, [ReasonMaxLength] string? reason = null)
            {
                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                int affectedRows = await db.LevelingRoles.Where(role => role.GuildId == Context.Guild.Id && role.Level == level).ExecuteDeleteAsync();

                if (affectedRows == 0)
                {
                    await FollowupAsync(AlreadyConfiguredMessage);
                    return;
                }

                await LogAsync(db, Context, reason);
            }

            [SlashCmd("View the configured auto roles")]
            public async Task ViewAutoRoles()
            {
                await DeferAsync();

                string? autoRolesText = null;
                bool isDisabled = false;

                Stopwatch stopwatch;
                await using (ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync())
                {
                    await db.Database.OpenConnectionAsync();
                    await using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
                    {
                        cmd.CommandText = """
                                          SELECT
                                              g."Configuration_Leveling_IsDisabled" AS "IsDisabled",
                                              string_agg(
                                                  '- Level ' || r."Level" || ': <@&' || r."Id" || '>' ||
                                                  COALESCE(' (x' || trim(trailing '.' from trim(trailing '0' from to_char(r."Multiplier", 'FM999999999.##'))) || ')', '') ||
                                                  COALESCE(' (' || r."Cooldown" || 's)', ''),
                                                  E'\n'
                                              ORDER BY r."Level"
                                              ) AS "AutoRoles"
                                          FROM "Guilds" g
                                          LEFT JOIN "LevelingRoles" r ON r."GuildId" = g."Id"
                                          WHERE g."Id" = @guildId
                                          GROUP BY g."Configuration_Leveling_IsDisabled";
                                          """;

                        cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));

                        stopwatch = Stopwatch.StartNew();
                        await using (DbDataReader reader =
                                     await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.CloseConnection))
                        {
                            if (await reader.ReadAsync())
                            {
                                isDisabled = reader.GetBoolean(0);
                                autoRolesText = reader.IsDBNull(1) ? null : reader.GetString(1);
                            }

                            stopwatch.Stop();
                        }
                    }
                }

                Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);

                if (String.IsNullOrWhiteSpace(autoRolesText))
                {
                    await FollowupAsync(NothingToView, ephemeral: true);
                    return;
                }

                var embedBuilder = new EmbedBuilder
                {
                    Title = $"{Context.Guild.Name} Leveling Auto Roles",
                    Color = Storage.LightGold,
                    Description = autoRolesText
                };

                if (isDisabled)
                {
                    embedBuilder.Footer = new EmbedFooterBuilder { Text = "Module is currently disabled" };
                }

                await FollowupAsync(embeds: [embedBuilder.Build()]);
            }
        }
    }
}
