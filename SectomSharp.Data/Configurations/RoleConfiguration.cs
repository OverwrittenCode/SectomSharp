using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Models;

namespace SectomSharp.Data.Configurations;

public sealed class RoleConfiguration : BaseEntityConfiguration<Role>
{
    public override void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasOne(role => role.Guild).WithMany(guild => guild.Roles).HasForeignKey(role => role.GuildId).IsRequired();
        base.Configure(builder);
    }
}
