using JetBrains.Annotations;

namespace SectomSharp.Data.Models;

public sealed class Guild : BaseEntity
{
    public required ulong Id { get; init; }

    [UsedImplicitly]
    public ICollection<User> Users { get; } = [];

    [UsedImplicitly]
    public ICollection<Role> Roles { get; } = [];

    [UsedImplicitly]
    public ICollection<Channel> Channels { get; } = [];

    public ICollection<AuditLogChannel> AuditLogChannels { get; } = [];
    public ICollection<BotLogChannel> BotLogChannels { get; } = [];
    public ICollection<Case> Cases { get; } = [];

    public Configuration? Configuration { get; set; }
}
