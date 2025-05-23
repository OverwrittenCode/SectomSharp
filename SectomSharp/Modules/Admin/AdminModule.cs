using Discord;
using Discord.Interactions;
using SectomSharp.Attributes;

namespace SectomSharp.Modules.Admin;

[Category(nameof(Admin), "⚙️")]
[RateLimit]
[CommandContextType(InteractionContextType.Guild)]
[DefaultMemberPermissions(GuildPermission.Administrator)]
public sealed partial class AdminModule : BaseModule;
