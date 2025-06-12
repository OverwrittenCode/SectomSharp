using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;

namespace SectomSharp.Modules.Admin;

[Category(nameof(Admin), "⚙️")]
[RateLimit]
[CommandContextType(InteractionContextType.Guild)]
[DefaultMemberPermissions(GuildPermission.Administrator)]
public sealed partial class AdminModule : BaseModule<AdminModule>
{
    /// <inheritdoc />
    public AdminModule(ILogger<AdminModule> logger) : base(logger) { }
}
