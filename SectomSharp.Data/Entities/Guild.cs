using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SectomSharp.Data.Entities;

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

public sealed class GuildConfiguration : BaseEntityConfiguration<Guild>
{
    public override void Configure(EntityTypeBuilder<Guild> builder)
    {
        builder.OwnsOne(
            guild => guild.Configuration,
            configBuilder =>
            {
                configBuilder.ToJson();
                configBuilder.OwnsOne(configuration => configuration.Warning, warningBuilder => warningBuilder.OwnsMany(warning => warning.Thresholds));
            }
        );

        builder.HasIndex(guild => guild.Id).IsUnique();
        base.Configure(builder);
    }
}
