using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DAL.DesignTime;

public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        const string postgresConnectionString = "Host=localhost;Port=5432;Database=tightwiki_db;Username=postgres;Password=123456";
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseNpgsql(postgresConnectionString);
        return new IdentityDbContext(optionsBuilder.Options);
    }
}
