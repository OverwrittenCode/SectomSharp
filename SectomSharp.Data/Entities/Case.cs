using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.CompositeTypes;
using SectomSharp.Data.Enums;
using SectomSharp.Data.Extensions;

namespace SectomSharp.Data.Entities;

public sealed class Case : BaseOneToManyGuildRelation
{
    public required string Id { get; init; }

    public ulong? PerpetratorId { get; init; }
    public string? PerpetratorAvatarUrl { get; init; }
    public User? Perpetrator { get; init; }

    public ulong? TargetId { get; init; }
    public User? Target { get; init; }

    public ulong? ChannelId { get; init; }
    public Channel? Channel { get; init; }

    public required BotLogType LogType { get; init; }
    public required OperationType OperationType { get; init; }
    public required string Description { get; init; }

    public required uint Color { get; init; }

    [NotMapped]
    public required CompositeEmbedField[] Fields { get; init; }

    public string? Reason { get; init; }
    public string? LogMessageUrl { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}

public sealed class CaseConfiguration : BaseOneToManyGuildRelationConfiguration<Case>
{
    private const int LogMessageUrlMaxLength = 96;
    private const int DescriptionMaxLength = Byte.MaxValue;
    private const int PerpetratorAvatarUrlMaxLength = Byte.MaxValue;

    public const int IdLength = 6;
    public const int ReasonMaxLength = Byte.MaxValue;

    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<Case> builder)
    {
        builder.Property(@case => @case.PerpetratorId).IsSnowflakeId();
        builder.Property(@case => @case.PerpetratorAvatarUrl).HasMaxLength(PerpetratorAvatarUrlMaxLength);
        builder.HasOne(@case => @case.Perpetrator).WithMany().HasForeignKey(@case => new { @case.GuildId, @case.PerpetratorId });

        builder.Property(@case => @case.TargetId).IsSnowflakeId();
        builder.HasOne(@case => @case.Target).WithMany().HasForeignKey(@case => new { @case.GuildId, @case.TargetId });

        builder.Property(@case => @case.ChannelId).IsSnowflakeId();
        builder.HasOne(@case => @case.Channel).WithMany().HasForeignKey(@case => @case.ChannelId);

        builder.Property(@case => @case.LogType).IsRequired();
        builder.Property(@case => @case.OperationType).IsRequired();
        builder.Property(@case => @case.Description).IsRequired().HasMaxLength(DescriptionMaxLength);
        builder.Property(@case => @case.Color).IsRequiredNonNegativeInt();

        builder.Property(@case => @case.Id).HasMaxLength(IdLength);
        builder.Property(@case => @case.Reason).HasMaxLength(ReasonMaxLength);
        builder.Property(@case => @case.LogMessageUrl).HasMaxLength(LogMessageUrlMaxLength);

        builder.HasIndex(@case => new { @case.GuildId, @case.TargetId })
               .HasFilter(
                    $"""
                     "{nameof(Case.LogType)}" = {(int)BotLogType.Warn} AND "{nameof(Case.OperationType)}" = {(int)OperationType.Create} 
                     """
                )
               .HasDatabaseName("IX_Cases_GuildId_TargetId_Warn_Create");

        builder.HasKey(@case => new { @case.GuildId, @case.Id });
        base.Configure(builder);
    }
}
