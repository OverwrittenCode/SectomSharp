using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Data.Entities;
using SectomSharp.Data.Enums;
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
            /// <inheritdoc />
            protected DisableableModule(ILogger<BaseModule<TThis>> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(logger, dbContextFactory) { }

            protected abstract Task SetIsDisabledAsync(bool isDisabled, string? reason);

            [SlashCmd("Disable this configuration")]
            public async Task Disable([ReasonMaxLength] string? reason = null) => await SetIsDisabledAsync(true, reason);

            [SlashCmd("Enable this configuration")]
            public async Task Enable([ReasonMaxLength] string? reason = null) => await SetIsDisabledAsync(false, reason);
        }
    }
}
