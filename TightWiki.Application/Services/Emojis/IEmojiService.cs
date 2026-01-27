using TightWiki.Contracts;
using TightWiki.Contracts.DataModels;

namespace BLL.Services.Emojis
{
    /// <summary>
    /// Service interface for emoji business operations.
    /// All emoji-related operations should go through this service.
    /// </summary>
    public interface IEmojiService
    {
        /// <summary>
        /// Gets all emojis ordered by name.
        /// </summary>
        List<Emoji> GetAllEmojis();

        /// <summary>
        /// Gets emojis with pagination and optional filtering/sorting.
        /// </summary>
        List<Emoji> GetAllEmojisPaged(
            int pageNumber,
            string? orderBy = null,
            string? orderByDirection = null,
            List<string>? categories = null);

        /// <summary>
        /// Gets a single emoji by name.
        /// </summary>
        Emoji? GetEmojiByName(string name);

        /// <summary>
        /// Gets emoji categories for a specific emoji name.
        /// </summary>
        List<EmojiCategory> GetEmojiCategoriesByName(string name);

        /// <summary>
        /// Creates or updates an emoji.
        /// </summary>
        int UpsertEmoji(UpsertEmoji emoji);

        /// <summary>
        /// Deletes an emoji by its ID.
        /// </summary>
        void DeleteById(int id);

        /// <summary>
        /// Checks if an emoji name already exists.
        /// </summary>
        bool EmojiNameExists(string name);

        /// <summary>
        /// Autocomplete search for emoji shortcuts.
        /// </summary>
        IEnumerable<string> AutoCompleteEmoji(string term);

        /// <summary>
        /// Gets emojis filtered by a specific category.
        /// </summary>
        IEnumerable<Emoji> GetEmojisByCategory(string category);

        /// <summary>
        /// Gets emoji categories grouped with counts.
        /// </summary>
        IEnumerable<EmojiCategory> GetEmojiCategoriesGrouped();

        /// <summary>
        /// Reloads the emoji cache. Called during startup and after emoji modifications.
        /// Populates GlobalConfiguration.Emojis and pre-loads animated emojis if configured.
        /// </summary>
        void ReloadEmojisCache();
    }
}

