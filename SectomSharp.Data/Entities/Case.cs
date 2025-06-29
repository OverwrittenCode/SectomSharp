using Discord;
using Discord.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using SectomSharp.Data.Enums;
using SectomSharp.Data.Extensions;

namespace SectomSharp.Data.Entities;

public sealed class Case : BaseOneToManyGuildRelation
{
    public required string Id { get; init; }

    public ulong? PerpetratorId { get; init; }
    public User? Perpetrator { get; init; }

    public ulong? TargetId { get; init; }
    public User? Target { get; init; }

    public ulong? ChannelId { get; init; }
    public Channel? Channel { get; init; }

    public required BotLogType LogType { get; init; }
    public required OperationType OperationType { get; init; }

    public required EmbedBuilder CommandInputEmbedBuilder { get; init; }

    public DateTime? ExpiresAt { get; init; }
    public string? Reason { get; init; }
    public string? LogMessageUrl { get; set; }
}

public sealed class CaseConfiguration : BaseOneToManyGuildRelationConfiguration<Case>
{
    private const int LogMessageUrlMaxLength = 128;

    public const int IdLength = 6;
    public const int ReasonMaxLength = 255;

    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<Case> builder)
    {
        builder.Property(c => c.PerpetratorId).IsSnowflakeId();
        builder.Property(c => c.TargetId).IsSnowflakeId();
        builder.Property(c => c.ChannelId).IsSnowflakeId();

        builder.HasOne(@case => @case.Guild).WithMany(guild => guild.Cases).HasForeignKey(@case => @case.GuildId).IsRequired();

        builder.HasOne(@case => @case.Perpetrator)
       .WithMany(user => user.PerpetratorCases)
       .HasForeignKey(@case => new
            {
                @case.GuildId,
                @case.PerpetratorId
            }
        );
        builder.HasOne(@case => @case.Target)
       .WithMany(user => user.TargetCases)
       .HasForeignKey(@case => new
            {
                @case.GuildId,
                @case.TargetId
            }
        );

        builder.HasOne(@case => @case.Channel).WithMany(channel => channel.Cases).HasForeignKey(@case => @case.ChannelId);

        builder.Property(@case => @case.Reason).HasMaxLength(ReasonMaxLength);
        builder.Property(@case => @case.Id).HasMaxLength(IdLength);
        builder.Property(@case => @case.LogMessageUrl).HasMaxLength(LogMessageUrlMaxLength);

        builder.Property(@case => @case.ExpiresAt).HasColumnType(Constants.PostgreSql.Timestamptz);
        builder.Property(@case => @case.LogType).IsRequired();
        builder.Property(@case => @case.OperationType).IsRequired();
        builder.Property(@case => @case.CommandInputEmbedBuilder)
               .HasConversion(embedBuilder => embedBuilder.ToJsonString(Formatting.None), json => EmbedBuilderUtils.Parse(json))
               .HasColumnType(Constants.PostgreSql.JsonB)
               .IsRequired();

        builder.HasIndex(@case => new
                    {
                        @case.GuildId,
                        @case.TargetId
                    }
                )
               .HasFilter($"""    "{nameof(Case.LogType)}" = {(int)BotLogType.Warn} AND "{nameof(Case.OperationType)}" = {(int)OperationType.Create}""")
               .HasDatabaseName("IX_Cases_GuildId_TargetId_Warn_Create");

        builder.HasKey(@case => new
            {
                @case.GuildId,
                @case.Id
            }
        );

        base.Configure(builder);
    }
}
