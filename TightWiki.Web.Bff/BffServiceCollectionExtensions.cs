using Microsoft.Extensions.DependencyInjection;
using TightWiki.Web.Bff.Interfaces;
using TightWiki.Web.Bff.Services;

namespace TightWiki.Web.Bff
{
    /// <summary>
    /// Extension methods for registering BFF services in DI container.
    /// </summary>
    public static class BffServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Web BFF services.
        /// Call this in the host application's Program.cs.
        /// </summary>
        public static IServiceCollection AddWebBffServices(this IServiceCollection services)
        {
            // Register BFF orchestration services
            services.AddScoped<IPageBffService, PageBffService>();
            // services.AddScoped<IUserBffService, UserBffService>();
            // services.AddScoped<IFileBffService, FileBffService>();

            return services;
        }
    }
}
