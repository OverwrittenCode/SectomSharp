using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Enums;
using SectomSharp.Data.Models;

namespace SectomSharp.Data.Configurations;

internal sealed class SnowflakeConfiguration : BaseEntityConfiguration<Snowflake>
{
    public override void Configure(EntityTypeBuilder<Snowflake> builder)
    {
        builder
            .HasDiscriminator<SnowflakeType>("Type")
            .HasValue<Snowflake>(SnowflakeType.None)
            .HasValue<User>(SnowflakeType.User)
            .HasValue<Role>(SnowflakeType.Role)
            .HasValue<Channel>(SnowflakeType.Channel)
            .HasValue<BotLogChannel>(SnowflakeType.BotLogChannel)
            .HasValue<AuditLogChannel>(SnowflakeType.AuditLogChannel);

        base.Configure(builder);
    }
}
