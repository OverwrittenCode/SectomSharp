using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Discord.Interactions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;
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
        private const string AtLeastOneMessage = "At least one new value must be provided.";

        private static Task LogCreateAsync(ApplicationDbContext db, SocketInteractionContext context, string? reason = null, ulong? channelId = null)
            => CaseUtils.LogAsync(db, context, BotLogType.Configuration, OperationType.Create, channelId: channelId, reason: reason);

        private static Task LogUpdateAsync(ApplicationDbContext db, SocketInteractionContext context, string? reason = null, ulong? channelId = null)
            => CaseUtils.LogAsync(db, context, BotLogType.Configuration, OperationType.Update, channelId: channelId, reason: reason);

        private static Task LogDeleteAsync(ApplicationDbContext db, SocketInteractionContext context, string? reason = null, ulong? channelId = null)
            => CaseUtils.LogAsync(db, context, BotLogType.Configuration, OperationType.Delete, channelId: channelId, reason: reason);

        /// <inheritdoc />
        public ConfigModule(ILogger<ConfigModule> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(logger, dbContextFactory) { }

        /// <summary>
        ///     Provides a base class for a disableable config command module to inherit from.
        /// </summary>
        /// <typeparam name="TThis">Type of interaction context to be injected into the module.</typeparam>
        public abstract class DisableableModule<TThis> : BaseModule<TThis>
            where TThis : DisableableModule<TThis>, IDisableableModule<TThis>
        {
            [LanguageInjection("SQL")] [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
            private static readonly string DisableQuery = $"""
                                                           INSERT INTO "Guilds" ("Id", "{TThis.DisableColumnName}")
                                                           VALUES (@guildId, @isDisabled)
                                                           ON CONFLICT ("Id") DO UPDATE SET "{TThis.DisableColumnName}" = @isDisabled
                                                           WHERE "Guilds"."{TThis.DisableColumnName}" IS DISTINCT FROM @isDisabled
                                                           RETURNING 1
                                                           """;

            /// <inheritdoc />
            protected DisableableModule(ILogger<TThis> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(logger, dbContextFactory) { }

            private async Task SetIsDisabledAsync(bool isDisabled, string? reason)
            {
                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                await db.Database.OpenConnectionAsync();
                object? scalarResult;
                Stopwatch stopwatch;
                await using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = DisableQuery;
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

                await LogUpdateAsync(db, Context, reason);
            }

            [SlashCmd("Disable this configuration")]
            public Task Disable([ReasonMaxLength] string? reason = null) => SetIsDisabledAsync(true, reason);

            [SlashCmd("Enable this configuration")]
            public Task Enable([ReasonMaxLength] string? reason = null) => SetIsDisabledAsync(false, reason);
        }

        public interface IDisableableModule<TThis>
            where TThis : IDisableableModule<TThis>
        {
            /// <summary>
            ///     Gets the full SQL column name in the <c>Guilds</c> table that controls whether this module is disabled.
            /// </summary>
            [LanguageInjection(
                "sql",
                Prefix = "SELECT \"",
                Suffix = """
                         " FROM "Guilds" 
                         """
            )]
            static abstract string DisableColumnName { get; }
        }
    }
}
