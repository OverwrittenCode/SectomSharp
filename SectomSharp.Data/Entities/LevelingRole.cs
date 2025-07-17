using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Extensions;

namespace SectomSharp.Data.Entities;

public sealed class LevelingRole : Snowflake
{
    public required uint Level { get; init; }
    public double? Multiplier { get; init; }
    public uint? Cooldown { get; init; }
}

public sealed class LevelingRoleConfiguration : SnowflakeConfiguration<LevelingRole>
{
    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<LevelingRole> builder)
    {
        builder.Property(role => role.Level).IsRequiredNonNegativeInt();
        builder.Property(role => role.Cooldown).IsNonNegativeInt();
        builder.HasIndex(role => new { role.GuildId, role.Level });
        builder.HasIndex(role => new { role.GuildId, role.Id }).IncludeProperties(role => new { role.Cooldown, role.Multiplier });
        base.Configure(builder);
    }
}
