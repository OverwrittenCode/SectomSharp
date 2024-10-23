using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Models;

namespace SectomSharp.Data.Configurations;

internal sealed class AuditLogChannelConfiguration : BaseEntityConfiguration<AuditLogChannel>
{
    public override void Configure(EntityTypeBuilder<AuditLogChannel> builder)
    {
        builder
            .HasOne(channel => channel.Guild)
            .WithMany(guild => guild.AuditLogChannels)
            .HasForeignKey(channel => channel.GuildId)
            .IsRequired();

        builder.Property(channel => channel.AuditLogType).IsRequired();

        base.Configure(builder);
    }
}
