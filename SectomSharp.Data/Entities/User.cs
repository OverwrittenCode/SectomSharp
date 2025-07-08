using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Extensions;

namespace SectomSharp.Data.Entities;

public sealed class User : Snowflake
{
    [UsedImplicitly]
    public ICollection<Case> TargetCases { get; } = [];

    public UserLevel Level { get; } = new();

    [UsedImplicitly]
    public ICollection<Case> PerpetratorCases { get; private set; } = [];
}

public sealed class UserConfiguration : SnowflakeConfiguration<User>
{
    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasOne(user => user.Guild).WithMany(guild => guild.Users).HasForeignKey(user => user.GuildId).IsRequired();
        builder.OwnsOne(
            user => user.Level,
            levelBuilder =>
            {
                levelBuilder.Property(level => level.CurrentXp).IsRequiredNonNegativeInt().HasDefaultValue(0);
                levelBuilder.Property(level => level.UpdatedAt).HasColumnType(Constants.PostgreSql.Timestamptz);
            }
        );
        builder.HasKey(user => new
            {
                user.GuildId,
                user.Id
            }
        );
        base.Configure(builder);
    }
}
