using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Models;

namespace SectomSharp.Data.Configurations;

internal sealed class GuildConfiguration : BaseEntityConfiguration<Guild>
{
    public override void Configure(EntityTypeBuilder<Guild> builder)
    {
        builder.OwnsOne(
            guild => guild.Configuration,
            configBuilder =>
            {
                configBuilder.ToJson();
                configBuilder.OwnsOne(configuration => configuration.Warning, warningBuilder => warningBuilder.OwnsMany(warning => warning.Thresholds));
            }
        );

        builder.HasIndex(guild => guild.Id).IsUnique();

        base.Configure(builder);
    }
}
