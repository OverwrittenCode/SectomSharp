using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SectomSharp.Data.Entities;

public sealed class User : Snowflake
{
    [UsedImplicitly]
    public ICollection<Case> TargetCases { get; } = [];

    [UsedImplicitly]
    public ICollection<Case> PerpetratorCases { get; private set; } = [];
}

public sealed class UserConfiguration : BaseEntityConfiguration<User>
{
    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasOne(user => user.Guild).WithMany(guild => guild.Users).HasForeignKey(user => user.GuildId).IsRequired();
        base.Configure(builder);
    }
}
