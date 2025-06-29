using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SectomSharp.Data;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets(Assembly.Load(nameof(SectomSharp))).Build();
        string connectionString = config["PostgreSQL:ConnectionString"] ?? throw new InvalidOperationException("Missing PostgreSQL connection string");

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options;
        return new ApplicationDbContext(options);
    }
}
