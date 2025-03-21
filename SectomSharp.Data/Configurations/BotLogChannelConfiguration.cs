using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Models;

namespace SectomSharp.Data.Configurations;

public sealed class BotLogChannelConfiguration : BaseEntityConfiguration<BotLogChannel>
{
    public override void Configure(EntityTypeBuilder<BotLogChannel> builder)
    {
        builder.HasOne(channel => channel.Guild).WithMany(guild => guild.BotLogChannels).HasForeignKey(channel => channel.GuildId).IsRequired();

        builder.Property(channel => channel.Type).IsRequired();

        base.Configure(builder);
    }
}
