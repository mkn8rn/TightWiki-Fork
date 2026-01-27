using TightWiki.Contracts;

namespace BLL.Services.Configuration
{
    /// <summary>
    /// Provides engine configuration by reading from GlobalConfiguration cache.
    /// This serves as a bridge between the legacy static GlobalConfiguration
    /// and the new injectable IEngineConfiguration pattern.
    /// </summary>
    public class EngineConfigurationProvider : IEngineConfigurationProvider
    {
        /// <summary>
        /// Gets the current engine configuration for wiki transformation.
        /// Reads from the cached GlobalConfiguration values.
        /// </summary>
        public IEngineConfiguration GetEngineConfiguration()
        {
            return new EngineConfiguration
            {
                BasePath = GlobalConfiguration.BasePath,
                SiteName = GlobalConfiguration.Name,
                RecordCompilationMetrics = GlobalConfiguration.RecordCompilationMetrics,
                EnablePublicProfiles = GlobalConfiguration.EnablePublicProfiles,
                Emojis = GlobalConfiguration.Emojis
            };
        }
    }
}
