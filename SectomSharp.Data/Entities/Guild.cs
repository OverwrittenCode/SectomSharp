using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Extensions;

namespace SectomSharp.Data.Entities;

public sealed class Guild : BaseEntity, ISnowflakeId
{
    public required ulong Id { get; init; }

    [UsedImplicitly]
    public ICollection<User> Users { get; } = [];

    [UsedImplicitly]
    public ICollection<Channel> Channels { get; } = [];

    public ICollection<AuditLogChannel> AuditLogChannels { get; } = [];
    public ICollection<BotLogChannel> BotLogChannels { get; } = [];
    public ICollection<Case> Cases { get; } = [];

    public ICollection<LevelingRole> LevelingRoles { get; } = [];
    public ICollection<WarningThreshold> WarningThresholds { get; } = [];

    public Configuration Configuration { get; init; } = new();
}

public sealed class GuildConfiguration : SnowflakeIdConfiguration<Guild>
{
    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<Guild> builder)
    {
        builder.OwnsOne(
            guild => guild.Configuration,
            configBuilder =>
            {
                configBuilder.OwnsOne(
                    configuration => configuration.Warning,
                    warningBuilder => warningBuilder.Property(warning => warning.IsDisabled).IsRequired().HasDefaultValue(false)
                );
                configBuilder.OwnsOne(
                    configuration => configuration.Leveling,
                    levelBuilder =>
                    {
                        levelBuilder.Property(level => level.IsDisabled).IsRequired().HasDefaultValue(false);
                        levelBuilder.Property(level => level.GlobalMultiplier).IsRequired().HasDefaultValue(1).ValueGeneratedOnAdd();
                        levelBuilder.Property(level => level.GlobalCooldown).IsRequiredNonNegativeInt().HasDefaultValue(3).ValueGeneratedOnAdd();
                    }
                );
            }
        );

        base.Configure(builder);
    }
}
