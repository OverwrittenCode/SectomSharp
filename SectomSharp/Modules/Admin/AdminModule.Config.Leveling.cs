using System.Data.Common;
using System.Text;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Data.Entities;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Admin;

public sealed partial class AdminModule
{
    public sealed partial class ConfigModule
    {
        [Group("leveling", "Leveling configuration")]
        public sealed class LevelingModule : DisableableModule<LevelingModule, LevelingConfiguration>
        {
            private static readonly Func<ApplicationDbContext, ulong, Task<GuildAutoRolesView?>> TryGetAutoRoles = EF.CompileAsyncQuery((ApplicationDbContext db, ulong guildId)
                => db.Guilds.Where(guild => guild.Id == guildId)
                     .Select(guild => new GuildAutoRolesView(
                              guild.Configuration.Leveling.IsDisabled,
                              guild.LevelingRoles.OrderBy(role => role.Level).Select(role => new AutoRoleView(role.Id, role.Level, role.Multiplier, role.Cooldown))
                          )
                      )
                     .FirstOrDefault()
            );

            /// <inheritdoc />
            public LevelingModule(ILogger<BaseModule<LevelingModule>> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(logger, dbContextFactory) { }

            private async Task ModifySettingsAsync(string? reason, bool? accumulateMultipliers = null, double? globalMultiplier = null, uint? globalCooldown = null)
            {
                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                await db.Database.OpenConnectionAsync();
                await using DbCommand cmd = db.Database.GetDbConnection().CreateCommand();

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

                if (await cmd.ExecuteScalarAsync() is null)
                {
                    await RespondOrFollowupAsync(AlreadyConfiguredMessage);
                    return;
                }

                await LogAsync(db, Context, reason);
            }

            [SlashCmd("Set if multipliers should accumulate")]
            public async Task SetAccumulateMultipliers(bool accumulateMultipliers, [ReasonMaxLength] string? reason = null)
                => await ModifySettingsAsync(reason, accumulateMultipliers);

            [SlashCmd("Set the global multiplier")]
            public async Task SetGlobalMultiplier([MinValue(1)] double globalMultiplier, [ReasonMaxLength] string? reason = null)
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
                await using DbCommand cmd = db.Database.GetDbConnection().CreateCommand();

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

                if (await cmd.ExecuteScalarAsync() is null)
                {
                    await RespondOrFollowupAsync(AlreadyConfiguredMessage);
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
                    await RespondOrFollowupAsync(AlreadyConfiguredMessage);
                    return;
                }

                await LogAsync(db, Context, reason);
            }

            [SlashCmd("View the configured auto roles")]
            public async Task ViewAutoRoles()
            {
                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                GuildAutoRolesView? result = await TryGetAutoRoles(db, Context.Guild.Id);

                if (result?.AutoRoles.Any() != true)
                {
                    await RespondOrFollowupAsync(NothingToView);
                    return;
                }

                EmbedBuilder embedBuilder = GetConfigurationEmbedBuilder(result.IsDisabled);

                embedBuilder.WithTitle($"{Context.Guild.Name} Leveling Auto Roles")
                            .WithDescription(
                                 String.Join(
                                     '\n',
                                     result.AutoRoles.Select(autoRole =>
                                         {
                                             var builder = new StringBuilder($"- Level {autoRole.Level}: {MentionUtils.MentionRole(autoRole.Id)}", 50);
                                             if (autoRole.Multiplier.HasValue)
                                             {
                                                 builder.Append($" (x{autoRole.Multiplier.Value:0.##})");
                                             }

                                             if (autoRole.Cooldown.HasValue)
                                             {
                                                 builder.Append($" ({autoRole.Cooldown.Value}s)");
                                             }

                                             return builder.ToString();
                                         }
                                     )
                                 )
                             );

                await RespondOrFollowupAsync(embeds: [embedBuilder.Build()]);
            }

            private sealed record AutoRoleView(ulong Id, uint Level, double? Multiplier, uint? Cooldown);
            private sealed record GuildAutoRolesView(bool IsDisabled, IEnumerable<AutoRoleView> AutoRoles);
        }
    }
}
