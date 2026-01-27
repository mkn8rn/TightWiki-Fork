using TightWiki.Contracts;
using TightWiki.Contracts.DataModels;

namespace BLL.Services.Configuration
{
    /// <summary>
    /// Service interface for configuration operations.
    /// </summary>
    public interface IConfigurationService
    {
        #region Configuration Entries

        /// <summary>
        /// Gets all configuration entries for a specific group.
        /// </summary>
        ConfigurationEntries GetConfigurationEntriesByGroupName(string groupName);

        /// <summary>
        /// Gets a specific configuration entry value by group and entry name.
        /// </summary>
        string? GetConfigurationValue(string groupName, string entryName);

        /// <summary>
        /// Gets a typed configuration value with automatic conversion.
        /// </summary>
        T? GetConfigurationValue<T>(string groupName, string entryName);

        /// <summary>
        /// Gets a typed configuration value with a default value if not found.
        /// </summary>
        T GetConfigurationValue<T>(string groupName, string entryName, T defaultValue);

        /// <summary>
        /// Saves a configuration entry value.
        /// </summary>
        void SaveConfigurationValue(string groupName, string entryName, string value);

        /// <summary>
        /// Gets a flat list of all configuration entries.
        /// </summary>
        List<ConfigurationFlat> GetFlatConfiguration();

        /// <summary>
        /// Gets configuration entries organized in a nested structure by group.
        /// </summary>
        List<ConfigurationNest> GetConfigurationNest();

        #endregion

        #region Themes

        /// <summary>
        /// Gets all available themes.
        /// </summary>
        List<Theme> GetAllThemes();

        /// <summary>
        /// Gets a Theme by name.
        /// </summary>
        Theme? GetThemeByName(string themeName);

        #endregion

        #region Menu Items

        /// <summary>
        /// Gets all menu items with optional sorting.
        /// </summary>
        List<MenuItem> GetAllMenuItems(string? orderBy = null, string? orderByDirection = null);

        /// <summary>
        /// Gets a menu item by ID.
        /// </summary>
        MenuItem GetMenuItemById(int id);

        /// <summary>
        /// Deletes a menu item by ID.
        /// </summary>
        void DeleteMenuItem(int id);

        /// <summary>
        /// Updates an existing menu item.
        /// </summary>
        int UpdateMenuItem(MenuItem MenuItem);

        /// <summary>
        /// Creates a new menu item.
        /// </summary>
        int CreateMenuItem(MenuItem MenuItem);

        #endregion

        #region Crypto/Security

        /// <summary>
        /// Checks if encryption is properly configured.
        /// </summary>
        bool ValidateCryptoSetup();

        /// <summary>
        /// Initializes encryption configuration.
        /// </summary>
        void InitializeCryptoSetup();

        /// <summary>
        /// Determines if this is the first time the wiki has run.
        /// </summary>
        bool IsFirstRun();

        #endregion

        #region Database Statistics

        /// <summary>
        /// Gets wiki database statistics.
        /// </summary>
        WikiDatabaseStatistics GetDatabaseStatistics();

        #endregion

        #region Configuration Reload

        /// <summary>
        /// Reloads all configuration settings from the database into GlobalConfiguration.
        /// </summary>
        void ReloadAllConfiguration();

        #endregion
    }
}

