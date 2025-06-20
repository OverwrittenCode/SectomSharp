using Discord.Interactions;
using Microsoft.Extensions.Logging;
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
        private const string NothingToView = "Nothing to view yet.";

        private static async Task LogAsync(SocketInteractionContext context, string? reason = null, ulong? channelId = null)
            => await CaseUtils.LogAsync(context, BotLogType.Configuration, OperationType.Update, channelId: channelId, reason: reason);

        /// <inheritdoc />
        public ConfigModule(ILogger<ConfigModule> logger) : base(logger) { }
    }
}
