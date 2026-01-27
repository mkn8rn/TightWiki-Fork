using TightWiki.Contracts;

namespace BLL.Services.Configuration
{
    /// <summary>
    /// Provides engine configuration data for wiki transformation.
    /// This decouples the Engine from GlobalConfiguration static state.
    /// </summary>
    public interface IEngineConfigurationProvider
    {
        /// <summary>
        /// Gets the current engine configuration for wiki transformation.
        /// </summary>
        IEngineConfiguration GetEngineConfiguration();
    }
}
