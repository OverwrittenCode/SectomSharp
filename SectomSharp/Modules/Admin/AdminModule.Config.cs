using Discord.Interactions;
using SectomSharp.Data.Enums;
using SectomSharp.Services;

namespace SectomSharp.Modules.Admin;

public partial class AdminModule
{
    [Group("config", "Master configuration of the server")]
    public sealed partial class ConfigModule : BaseModule
    {
        internal const string AlreadyConfiguredMessage =
            "You cannot add this new configuration as there is already a matching configuration.";

        internal const string NotConfiguredMessage =
            "You cannot remove this configuration as it has not been configured.";

        internal static async Task LogAsync(
            SocketInteractionContext context,
            string? reason = null,
            ulong? channelId = null
        ) =>
            await CaseService.LogAsync(
                context,
                BotLogType.Configuration,
                OperationType.Update,
                reason: reason,
                channelId: channelId
            );
    }
}
