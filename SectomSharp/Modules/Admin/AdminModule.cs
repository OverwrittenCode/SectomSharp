using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;

namespace SectomSharp.Modules.Admin;

[Category(nameof(Admin), "⚙️")]
[RateLimit]
[CommandContextType(InteractionContextType.Guild)]
[DefaultMemberPermissions(GuildPermission.Administrator)]
public sealed partial class AdminModule : BaseModule<AdminModule>
{
    /// <inheritdoc />
    public AdminModule(ILogger<AdminModule> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(logger, dbContextFactory) { }
}
