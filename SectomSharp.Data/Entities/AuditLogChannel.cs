using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Enums;

namespace SectomSharp.Data.Entities;

public sealed class AuditLogChannel : Snowflake
{
    public required string WebhookUrl { get; init; }

    public required AuditLogType Type { get; set; }
}

public sealed class AuditLogChannelConfiguration : BaseEntityConfiguration<AuditLogChannel>
{
    private const int WebhookUrlLength = 255;

    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<AuditLogChannel> builder)
    {
        builder.HasOne(channel => channel.Guild).WithMany(guild => guild.AuditLogChannels).HasForeignKey(channel => channel.GuildId).IsRequired();
        builder.Property(channel => channel.Type).IsRequired();
        builder.Property(channel => channel.WebhookUrl).IsRequired().HasMaxLength(WebhookUrlLength);
        base.Configure(builder);
    }
}
