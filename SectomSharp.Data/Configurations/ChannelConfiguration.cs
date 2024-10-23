using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Models;

namespace SectomSharp.Data.Configurations;

internal sealed class ChannelConfiguration : BaseEntityConfiguration<Channel>
{
    public override void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder
            .HasOne(channel => channel.Guild)
            .WithMany(guild => guild.Channels)
            .HasForeignKey(channel => channel.GuildId)
            .IsRequired();

        base.Configure(builder);
    }
}
