using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Enums;
using SectomSharp.Data.Extensions;

namespace SectomSharp.Data.Entities;

public sealed class WarningThreshold : BaseOneToManyGuildRelation
{
    public required uint Value { get; init; }
    public required BotLogType LogType { get; init; }

    public TimeSpan? Span { get; init; }
}

public sealed class WarningThresholdConfiguration : BaseOneToManyGuildRelationConfiguration<WarningThreshold>
{
    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<WarningThreshold> builder)
    {
        builder.Property(threshold => threshold.Value).IsRequiredNonNegativeInt();
        builder.Property(threshold => threshold.LogType).IsRequired();
        builder.HasKey(threshold => new { threshold.GuildId, threshold.Value });
        base.Configure(builder);
    }
}
