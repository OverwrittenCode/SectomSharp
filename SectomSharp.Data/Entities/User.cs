using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Extensions;

namespace SectomSharp.Data.Entities;

public sealed class User : Snowflake
{
    public UserLevel Level { get; } = new();
}

public sealed class UserConfiguration : SnowflakeConfiguration<User>
{
    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        builder.OwnsOne(user => user.Level, levelBuilder => levelBuilder.Property(level => level.CurrentXp).IsRequiredNonNegativeInt().HasDefaultValue(0));
        builder.HasKey(user => new { user.GuildId, user.Id });
        base.Configure(builder);
    }
}
