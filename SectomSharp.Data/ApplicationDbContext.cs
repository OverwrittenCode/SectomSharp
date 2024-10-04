using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SectomSharp.Data.Configurations;
using SectomSharp.Data.Models;

namespace SectomSharp.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<Guild> Guilds { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new UserConfiguration());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddUserSecrets(typeof(ApplicationDbContext).Assembly)
            .Build();

        optionsBuilder.UseNpgsql(config["PostgreSQL:ConnectionString"]);

        base.OnConfiguring(optionsBuilder);
    }

    public override int SaveChanges()
    {
        var entities = ChangeTracker
            .Entries()
            .Where(entity => (entity.Entity is BaseEntity) && entity.State == EntityState.Modified);

        foreach (var entity in entities)
        {
            if (entity.Entity is BaseEntity baseEntity)
            {
                baseEntity.UpdatedAt = DateTime.Now;
            }
        }

        return base.SaveChanges();
    }
}
