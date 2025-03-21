using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Models;

namespace SectomSharp.Data.Configurations;

public sealed class AuditLogChannelConfiguration : BaseEntityConfiguration<AuditLogChannel>
{
    private const int WebhookUrlLength = 255;

    public override void Configure(EntityTypeBuilder<AuditLogChannel> builder)
    {
        builder.HasOne(channel => channel.Guild).WithMany(guild => guild.AuditLogChannels).HasForeignKey(channel => channel.GuildId).IsRequired();

        builder.Property(channel => channel.Type).IsRequired();

        builder.Property(channel => channel.WebhookUrl).IsRequired().HasMaxLength(WebhookUrlLength);

        base.Configure(builder);
    }
}
