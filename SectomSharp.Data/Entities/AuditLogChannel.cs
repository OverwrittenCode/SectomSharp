using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Enums;

namespace SectomSharp.Data.Entities;

public sealed class AuditLogChannel : Snowflake
{
    public required string WebhookUrl { get; init; }

    public required AuditLogType Type { get; init; }
}

public sealed class AuditLogChannelConfiguration : SnowflakeConfiguration<AuditLogChannel>
{
    private const int WebhookUrlLength = Byte.MaxValue;

    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<AuditLogChannel> builder)
    {
        builder.Property(channel => channel.Type).IsRequired();
        builder.Property(channel => channel.WebhookUrl).IsRequired().HasMaxLength(WebhookUrlLength);
        builder.HasIndex(c => new { c.GuildId, c.Id });
        base.Configure(builder);
    }
}
