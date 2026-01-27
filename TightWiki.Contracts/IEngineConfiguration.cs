using TightWiki.Contracts.DataModels;

namespace TightWiki.Contracts
{
    /// <summary>
    /// Configuration data needed by the Engine during wiki transformation.
    /// This is passed through ITightEngineState, not accessed via static globals.
    /// </summary>
    public interface IEngineConfiguration
    {
        /// <summary>
        /// Base URL path for the wiki (e.g., "" or "/wiki").
        /// Used for generating internal links and image URLs.
        /// </summary>
        string BasePath { get; }

        /// <summary>
        /// Name of the wiki site.
        /// </summary>
        string SiteName { get; }

        /// <summary>
        /// Whether to record compilation/transformation metrics.
        /// </summary>
        bool RecordCompilationMetrics { get; }

        /// <summary>
        /// Whether public user profiles are enabled.
        /// </summary>
        bool EnablePublicProfiles { get; }

        /// <summary>
        /// Cached emoji data for emoji processing.
        /// </summary>
        IReadOnlyList<Emoji> Emojis { get; }
    }

    /// <summary>
    /// Default implementation of IEngineConfiguration.
    /// </summary>
    public class EngineConfiguration : IEngineConfiguration
    {
        public string BasePath { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public bool RecordCompilationMetrics { get; set; }
        public bool EnablePublicProfiles { get; set; }
        public IReadOnlyList<Emoji> Emojis { get; set; } = [];
    }
}
