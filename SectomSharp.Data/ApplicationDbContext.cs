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
