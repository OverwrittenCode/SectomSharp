using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SectomSharp.Data.Configurations;
using SectomSharp.Data.Enums;
using SectomSharp.Data.Models;

namespace SectomSharp.Data;

[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public sealed class ApplicationDbContext : DbContext
{
#pragma warning disable CS0618 // Type or member is obsolete
    static ApplicationDbContext() =>
        NpgsqlConnection.GlobalTypeMapper.MapEnum<SnowflakeType>().MapEnum<OperationType>();
#pragma warning restore CS0618 // Type or member is obsolete

    public DbSet<Guild> Guilds { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Channel> Channels { get; set; } = null!;
    public DbSet<AuditLogChannel> AuditLogChannels { get; set; } = null!;
    public DbSet<BotLogChannel> BotLogChannels { get; set; } = null!;
    public DbSet<Case> Cases { get; set; } = null!;

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

    protected override void OnModelCreating(ModelBuilder builder) =>
        builder
            .HasPostgresEnum<SnowflakeType>()
            .HasPostgresEnum<OperationType>()
            .ApplyConfiguration(new GuildConfiguration())
            .ApplyConfiguration(new SnowflakeConfiguration())
            .ApplyConfiguration(new UserConfiguration())
            .ApplyConfiguration(new RoleConfiguration())
            .ApplyConfiguration(new ChannelConfiguration())
            .ApplyConfiguration(new AuditLogChannelConfiguration())
            .ApplyConfiguration(new BotLogChannelConfiguration())
            .ApplyConfiguration(new CaseConfiguration());

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddUserSecrets(typeof(ApplicationDbContext).Assembly)
            .Build();

        optionsBuilder.UseNpgsql(config["PostgreSQL:ConnectionString"]);

        base.OnConfiguring(optionsBuilder);
    }

    private void UpdateEntities()
    {
        IEnumerable<EntityEntry> entries = ChangeTracker
            .Entries()
            .Where(entry => entry is { Entity: BaseEntity, State: EntityState.Modified });

        foreach (EntityEntry entry in entries)
        {
            if (entry is { Entity: BaseEntity baseEntity, State: EntityState.Modified })
            {
                baseEntity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
