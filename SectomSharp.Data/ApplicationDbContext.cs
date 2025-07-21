using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using SectomSharp.Data.Entities;

namespace SectomSharp.Data;

[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public sealed class ApplicationDbContext : DbContext
{
    public DbSet<Guild> Guilds { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    public DbSet<Channel> Channels { get; set; } = null!;
    public DbSet<AuditLogChannel> AuditLogChannels { get; set; } = null!;
    public DbSet<BotLogChannel> BotLogChannels { get; set; } = null!;
    public DbSet<Case> Cases { get; set; } = null!;
    public DbSet<LevelingRole> LevelingRoles { get; set; } = null!;
    public DbSet<WarningThreshold> WarningThresholds { get; set; } = null!;
    public DbSet<SuggestionPanel> SuggestionPanels { get; set; } = null!;
    public DbSet<SuggestionComponent> SuggestionComponents { get; set; } = null!;
    public DbSet<SuggestionPost> SuggestionPosts { get; set; } = null!;
    public DbSet<SuggestionVote> SuggestionVotes { get; set; } = null!;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder) => builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
}
