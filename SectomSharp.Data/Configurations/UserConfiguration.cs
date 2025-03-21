using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Models;

namespace SectomSharp.Data.Configurations;

public sealed class UserConfiguration : BaseEntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasOne(user => user.Guild).WithMany(guild => guild.Users).HasForeignKey(user => user.GuildId).IsRequired();

        base.Configure(builder);
    }
}
