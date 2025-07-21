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
using SectomSharp.Extensions;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Admin;

public sealed partial class AdminModule
{
    public sealed partial class ConfigModule
    {
        [Group("leveling", "Leveling configuration")]
        public sealed class LevelingModule : DisableableModule<LevelingModule>, IDisableableModule<LevelingModule>
        {
            /// <inheritdoc />
            public static string DisableColumnName => "Configuration_Leveling_IsDisabled";

            /// <inheritdoc />
            public LevelingModule(ILogger<BaseModule<LevelingModule>> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(logger, dbContextFactory) { }

            [SlashCmd("Modify the settings")]
            public async Task ModifySettings(
                [Summary(description: "if multipliers should accumulate")] bool? accumulateMultipliers = null,
                [Summary(description: "the global multiplier")] [MinValue(1)] [MaxValue(10)] double? globalMultiplier = null,
                [Summary(description: "the global cooldown in seconds")] [MinValue(1)] uint? globalCooldown = null,
                [ReasonMaxLength] string? reason = null
            )
            {
                if (!accumulateMultipliers.HasValue && !globalMultiplier.HasValue && !globalCooldown.HasValue)
                {
                    await RespondAsync(AtLeastOneMessage);
                    return;
                }

                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                int affectedRows = await db.Guilds
                                           .Where(guild => guild.Id == Context.Guild.Id
                                                        && ((accumulateMultipliers.HasValue && accumulateMultipliers.Value != guild.Configuration.Leveling.AccumulateMultipliers)
                                                         || (globalMultiplier.HasValue && globalMultiplier.Value != guild.Configuration.Leveling.GlobalMultiplier)
                                                         || (globalCooldown.HasValue && globalCooldown.Value != guild.Configuration.Leveling.GlobalCooldown))
                                            )
                                           .ExecuteUpdateAsync(setPropertyCalls => setPropertyCalls
                                                                                  .SetProperty(
                                                                                       guild => guild.Configuration.Leveling.AccumulateMultipliers,
                                                                                       guild => accumulateMultipliers ?? guild.Configuration.Leveling.AccumulateMultipliers
                                                                                   )
                                                                                  .SetProperty(
                                                                                       guild => guild.Configuration.Leveling.GlobalMultiplier,
                                                                                       guild => globalMultiplier ?? guild.Configuration.Leveling.GlobalMultiplier
                                                                                   )
                                                                                  .SetProperty(
                                                                                       guild => guild.Configuration.Leveling.GlobalCooldown,
                                                                                       guild => globalCooldown ?? guild.Configuration.Leveling.GlobalCooldown
                                                                                   )
                                            );

                if (affectedRows == 0)
                {
                    await FollowupAsync(AlreadyConfiguredMessage);
                    return;
                }

                await LogAsync(db, Context, reason);
            }

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
