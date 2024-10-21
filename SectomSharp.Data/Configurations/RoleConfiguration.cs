using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Models;

namespace SectomSharp.Data.Configurations;

internal sealed class RoleConfiguration : BaseEntityConfiguration<Role>
{
    public override void Configure(EntityTypeBuilder<Role> builder)
    {
        builder
            .HasOne(role => role.Guild)
            .WithMany(guild => guild.Roles)
            .HasForeignKey(role => role.GuildId)
            .IsRequired();

        builder.HasIndex(role => role.Id).IsUnique();

        base.Configure(builder);
    }
}
