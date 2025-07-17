using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SectomSharp.Data.Entities;

public sealed class Channel : Snowflake;

public sealed class ChannelConfiguration : SnowflakeConfiguration<Channel>
{
    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.HasIndex(c => new { c.GuildId, c.Id });
        base.Configure(builder);
    }
}
