using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SectomSharp.Data.Entities;

public sealed class Role : Snowflake;

public sealed class RoleConfiguration : BaseEntityConfiguration<Role>
{
    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasOne(role => role.Guild).WithMany(guild => guild.Roles).HasForeignKey(role => role.GuildId).IsRequired();
        base.Configure(builder);
    }
}
