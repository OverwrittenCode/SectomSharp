using System.Data.Common;
using System.Runtime.CompilerServices;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Data.Entities;
using SectomSharp.Data.Enums;
using SectomSharp.Extensions;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Admin;

public sealed partial class AdminModule
{
    [Group("config", "Master configuration of the server")]
    public sealed partial class ConfigModule : BaseModule<ConfigModule>
    {
        private const string AlreadyConfiguredMessage = "You cannot add this new configuration as there is already a matching configuration.";
        private const string NotConfiguredMessage = "You cannot remove this configuration as it has not been configured.";

        private static async Task LogAsync(ApplicationDbContext db, SocketInteractionContext context, string? reason = null, ulong? channelId = null)
            => await CaseUtils.LogAsync(db, context, BotLogType.Configuration, OperationType.Update, channelId: channelId, reason: reason);

        /// <inheritdoc />
        public ConfigModule(ILogger<ConfigModule> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(logger, dbContextFactory) { }

        /// <summary>
        ///     Provides a base class for a disableable config command module to inherit from.
        /// </summary>
        /// <typeparam name="TThis">Type of interaction context to be injected into the module.</typeparam>
        /// <typeparam name="TConfig">Type of <see cref="BaseConfiguration" /> in <see cref="Configuration" />.</typeparam>
        public abstract class DisableableModule<TThis, TConfig> : BaseModule<TThis>
            where TConfig : BaseConfiguration
            where TThis : DisableableModule<TThis, TConfig>
        {
            private static string ModuleIsCurrentlyDisabledMessage
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    string path;
                    if (typeof(TConfig) == typeof(WarningConfiguration))
                    {
                        path = "/config warn enable";
                    }
                    else if (typeof(TConfig) == typeof(LevelingConfiguration))
                    {
                        path = "/config leveling enable";
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }

                    return $"Module is currently disabled: {path}";
                }
            }

            /// <inheritdoc />
            protected DisableableModule(ILogger<BaseModule<TThis>> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(logger, dbContextFactory) { }

            private async Task WarningSetIsDisabledAsync(bool isDisabled, string? reason)
            {
                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                await db.Database.OpenConnectionAsync();
                object? scalarResult;
                await using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = """
                                      INSERT INTO "Guilds" ("Id", "Configuration_Warning_IsDisabled")
                                      VALUES (@guildId, @isDisabled)
                                      ON CONFLICT ("Id") DO UPDATE
                                          SET "Configuration_Warning_IsDisabled" = @isDisabled
                                          WHERE "Guilds"."Configuration_Warning_IsDisabled" IS DISTINCT FROM @isDisabled
                                      RETURNING 1
                                      """;

                    cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromBoolean("isDisabled", isDisabled));

                    scalarResult = await cmd.ExecuteScalarTimedAsync(Logger);
                }

                if (scalarResult is null)
                {
                    await RespondOrFollowupAsync(AlreadyConfiguredMessage);
                    return;
                }

                await LogAsync(db, Context, reason);
            }

            private async Task LevelingSetIsDisabledAsync(bool isDisabled, string? reason)
            {
                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                await db.Database.OpenConnectionAsync();
                object? scalarResult;
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

                    scalarResult = await cmd.ExecuteScalarTimedAsync(Logger);
                }

                if (scalarResult is null)
                {
                    await RespondOrFollowupAsync(AlreadyConfiguredMessage);
                    return;
                }

                await LogAsync(db, Context, reason);
            }

            private async Task SetIsDisabledAsync(bool isDisabled, string? reason)
            {
                if (typeof(TConfig) == typeof(WarningConfiguration))
                {
                    await WarningSetIsDisabledAsync(isDisabled, reason);
                }
                else if (typeof(TConfig) == typeof(LevelingConfiguration))
                {
                    await LevelingSetIsDisabledAsync(isDisabled, reason);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            protected EmbedBuilder GetConfigurationEmbedBuilder(bool isDisabled)
            {
                EmbedBuilder embedBuilder = new EmbedBuilder().WithColor(Storage.LightGold);
                if (isDisabled)
                {
                    embedBuilder.WithFooter(ModuleIsCurrentlyDisabledMessage);
                }

                return embedBuilder;
            }

            [SlashCmd("Disable this configuration")]
            public async Task Disable([ReasonMaxLength] string? reason = null) => await SetIsDisabledAsync(true, reason);

            [SlashCmd("Enable this configuration")]
            public async Task Enable([ReasonMaxLength] string? reason = null) => await SetIsDisabledAsync(false, reason);

            protected sealed record ResultSet<T>(bool IsDisabled, IEnumerable<T> Items);
        }
    }
}
