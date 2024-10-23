using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SectomSharp.Data.Configurations;
using SectomSharp.Data.Enums;
using SectomSharp.Data.Models;

namespace SectomSharp.Data;

public sealed class ApplicationDbContext : DbContext
{
#pragma warning disable CS0618 // Type or member is obsolete
    static ApplicationDbContext() =>
        NpgsqlConnection
            .GlobalTypeMapper.MapEnum<SnowflakeType>()
            .MapEnum<OperationType>()
            .MapEnum<AuditLogType>()
            .MapEnum<BotLogType>();
#pragma warning restore CS0618 // Type or member is obsolete

    public DbSet<Guild> Guilds { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Channel> Channels { get; set; } = null!;
    public DbSet<AuditLogChannel> AuditLogChannels { get; set; } = null!;
    public DbSet<BotLogChannel> BotLogChannels { get; set; } = null!;
    public DbSet<Case> Cases { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder) =>
        builder
            .HasPostgresEnum<SnowflakeType>()
            .HasPostgresEnum<OperationType>()
            .HasPostgresEnum<AuditLogType>()
            .HasPostgresEnum<BotLogType>()
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
        var entries = ChangeTracker
            .Entries()
            .Where(entry =>
                entry is { Entity: BaseEntity, State: EntityState.Added or EntityState.Modified }
            );

        foreach (var entry in entries)
        {
            if (entry.Entity is BaseEntity baseEntity)
            {
                if (entry.State == EntityState.Modified)
                {
                    baseEntity.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
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
