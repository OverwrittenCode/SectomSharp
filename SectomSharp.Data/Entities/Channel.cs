using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SectomSharp.Data.Entities;

public sealed class Channel : Snowflake
{
    [UsedImplicitly]
    public ICollection<Case> Cases { get; } = [];
}

public sealed class ChannelConfiguration : SnowflakeConfiguration<Channel>
{
    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.HasOne(channel => channel.Guild).WithMany(guild => guild.Channels).HasForeignKey(channel => channel.GuildId).IsRequired();
        builder.HasIndex(c => new { c.GuildId, c.Id });
        base.Configure(builder);
    }
}
