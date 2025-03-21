using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using SectomSharp.Data.Configurations;
using SectomSharp.Data.Enums;
using SectomSharp.Data.Models;

namespace SectomSharp.Data;

[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public sealed class ApplicationDbContext : DbContext
{
    public DbSet<Guild> Guilds { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Channel> Channels { get; set; } = null!;
    public DbSet<AuditLogChannel> AuditLogChannels { get; set; } = null!;
    public DbSet<BotLogChannel> BotLogChannels { get; set; } = null!;
    public DbSet<Case> Cases { get; set; } = null!;

    private void UpdateEntities()
    {
        foreach (EntityEntry entry in ChangeTracker.Entries())
        {
            if (entry is { Entity: BaseEntity baseEntity, State: EntityState.Modified })
            {
                baseEntity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
        => builder.HasPostgresEnum<OperationType>()
                  .ApplyConfiguration(new GuildConfiguration())
                  .ApplyConfiguration(new UserConfiguration())
                  .ApplyConfiguration(new RoleConfiguration())
                  .ApplyConfiguration(new ChannelConfiguration())
                  .ApplyConfiguration(new AuditLogChannelConfiguration())
                  .ApplyConfiguration(new BotLogChannelConfiguration())
                  .ApplyConfiguration(new CaseConfiguration());

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets(typeof(ApplicationDbContext).Assembly).Build();
        optionsBuilder.UseNpgsql(config["PostgreSQL:ConnectionString"], builder => builder.MapEnum<OperationType>());

        base.OnConfiguring(optionsBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateEntities();
        return base.SaveChanges();
    }
}
