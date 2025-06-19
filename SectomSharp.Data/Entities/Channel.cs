using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SectomSharp.Data.Entities;

public sealed class Channel : Snowflake
{
    [UsedImplicitly]
    public ICollection<Case> Cases { get; } = [];
}

public sealed class ChannelConfiguration : BaseEntityConfiguration<Channel>
{
    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.HasOne(channel => channel.Guild).WithMany(guild => guild.Channels).HasForeignKey(channel => channel.GuildId).IsRequired();
        base.Configure(builder);
    }
}
