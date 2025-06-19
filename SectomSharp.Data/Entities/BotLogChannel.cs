using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Enums;

namespace SectomSharp.Data.Entities;

public sealed class BotLogChannel : Snowflake
{
    public required BotLogType Type { get; set; }
}

public sealed class BotLogChannelConfiguration : BaseEntityConfiguration<BotLogChannel>
{
    public override void Configure(EntityTypeBuilder<BotLogChannel> builder)
    {
        builder.HasOne(channel => channel.Guild).WithMany(guild => guild.BotLogChannels).HasForeignKey(channel => channel.GuildId).IsRequired();
        builder.Property(channel => channel.Type).IsRequired();
        base.Configure(builder);
    }
}
