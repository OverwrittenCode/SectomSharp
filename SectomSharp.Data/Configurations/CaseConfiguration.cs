using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Models;

namespace SectomSharp.Data.Configurations;

internal sealed class CaseConfiguration : BaseEntityConfiguration<Case>
{
    public override void Configure(EntityTypeBuilder<Case> builder)
    {
        builder
            .HasOne(@case => @case.Guild)
            .WithMany(guild => guild.Cases)
            .HasForeignKey(@case => @case.GuildId)
            .IsRequired();

        builder
            .HasOne(@case => @case.Perpetrator)
            .WithMany(user => user.PerpetratorCases)
            .HasForeignKey(@case => @case.PerpetratorId);

        builder
            .HasOne(@case => @case.Target)
            .WithMany(user => user.TargetCases)
            .HasForeignKey(@case => @case.TargetId);

        builder
            .HasOne(@case => @case.Channel)
            .WithMany(channel => channel.Cases)
            .HasForeignKey(@case => @case.ChannelId)
            .IsRequired();

        builder.Property(@case => @case.ExpiresAt).HasColumnType(Constants.PostgreSQL.Timestamptz);
        builder.Property(@case => @case.LogType).IsRequired();

        builder.HasIndex(@case => @case.Id).IsUnique();
        builder.HasIndex(@case => new
        {
            @case.GuildId,
            @case.TargetId,
            @case.LogType,
            @case.OperationType,
        });

        builder.HasKey(@case => new { @case.Id, @case.GuildId });

        base.Configure(builder);
    }
}