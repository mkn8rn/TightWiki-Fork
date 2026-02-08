using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DAL.DesignTime;

public sealed class WikiDbContextFactory : IDesignTimeDbContextFactory<WikiDbContext>
{
    public WikiDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WikiDbContext>();
        optionsBuilder.UseNpgsql(InfrastructureServiceRegistration.ConnectionString);
        return new WikiDbContext(optionsBuilder.Options);
    }
}
