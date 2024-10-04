using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Models;

namespace SectomSharp.Data.Configurations;

internal abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T>
    where T : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder
            .Property(entity => entity.CreatedAt)
            .HasColumnType(Constants.PostgreSQL.Timestamptz)
            .HasDefaultValueSql(Constants.PostgreSQL.CurrentDate);

        builder
            .Property(entity => entity.UpdatedAt)
            .HasColumnType(Constants.PostgreSQL.Timestamptz)
            .HasDefaultValueSql(Constants.PostgreSQL.CurrentDate);
    }
}
