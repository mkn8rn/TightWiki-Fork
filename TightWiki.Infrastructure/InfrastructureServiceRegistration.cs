using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DAL;

public static class InfrastructureServiceRegistration
{
    public const string ConnectionString =
        "Host=localhost;Port=5432;Database=tightwiki_db;Username=postgres;Password=123456";

    /// <summary>
    /// Registers <see cref="WikiDbContext"/> and <see cref="IdentityDbContext"/>.
    /// </summary>
    public static IServiceCollection AddTightWikiDbContexts(this IServiceCollection services)
    {
        services.AddDbContext<WikiDbContext>(options =>
        {
            options.UseNpgsql(ConnectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Wiki"));
        });

        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseNpgsql(ConnectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Identity"));
        });

        return services;
    }
}
