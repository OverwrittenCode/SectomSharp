namespace SectomSharp.Data.Models;

public sealed class Guild : BaseEntity
{
    public ICollection<User> Users { get; } = [];
    public ICollection<Role> Roles { get; } = [];
    public ICollection<Channel> Channels { get; } = [];
    public ICollection<AuditLogChannel> AuditLogChannels { get; } = [];
    public ICollection<BotLogChannel> BotLogChannels { get; } = [];
    public ICollection<Case> Cases { get; } = [];
    public required ulong Id { get; init; }

    public Configuration? Configuration { get; set; }
}
