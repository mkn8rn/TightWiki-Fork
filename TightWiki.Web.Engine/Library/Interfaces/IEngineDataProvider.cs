using TightWiki.Contracts;
using TightWiki.Contracts.DataModels;

namespace TightWiki.Web.Engine.Library.Interfaces
{
    /// <summary>
    /// Provides data access for the wiki Engine layer without coupling to repositories or BLL services.
    /// The Web/BFF layer implements this interface and passes it through ITightEngineState.
    /// </summary>
    public interface IEngineDataProvider
    {
        #region Page Operations

        /// <summary>
        /// Gets a page by navigation path and optional revision.
        /// </summary>
        EnginePage? GetPageByNavigation(string navigation, int? revision = null);

        /// <summary>
        /// Gets the most recently modified pages.
        /// </summary>
        List<EnginePage> GetTopRecentlyModifiedPages(int count);

        /// <summary>
        /// Gets pages within specified namespaces.
        /// </summary>
        List<EnginePage> GetPagesByNamespaces(List<string> namespaces);

        /// <summary>
        /// Gets pages with specified tags.
        /// </summary>
        List<EnginePage> GetPagesByTags(List<string> tags);

        /// <summary>
        /// Searches pages by search terms.
        /// </summary>
        List<EnginePage> SearchPages(List<string> searchTerms);

        /// <summary>
        /// Searches pages by search terms with pagination.
        /// </summary>
        List<EnginePage> SearchPagesPaged(List<string> searchTerms, int pageNumber, int? pageSize = null, bool? allowFuzzyMatching = null);

        /// <summary>
        /// Gets pages similar to the specified page.
        /// </summary>
        List<EngineRelatedPage> GetSimilarPagesPaged(int pageId, int similarity, int pageNumber, int? pageSize = null);

        /// <summary>
        /// Gets pages related to the specified page.
        /// </summary>
        List<EngineRelatedPage> GetRelatedPagesPaged(int pageId, int pageNumber, int? pageSize = null);

        /// <summary>
        /// Gets page revisions for a navigation path with pagination.
        /// </summary>
        List<EnginePageRevision> GetPageRevisionsInfoByNavigationPaged(string navigation, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);

        /// <summary>
        /// Gets tags associated with a seed tag.
        /// </summary>
        List<EngineTagAssociation> GetAssociatedTags(string seedTag);

        #endregion

        #region Page Persistence

        /// <summary>
        /// Saves a page (insert or update).
        /// </summary>
        int SavePage(EnginePage page);

        /// <summary>
        /// Updates the page reference for a navigation path.
        /// </summary>
        void UpdateSinglePageReference(string pageNavigation, int pageId);

        /// <summary>
        /// Updates page tags.
        /// </summary>
        void UpdatePageTags(int pageId, List<string> tags);

        /// <summary>
        /// Updates page processing instructions.
        /// </summary>
        void UpdatePageProcessingInstructions(int pageId, List<string> instructions);

        /// <summary>
        /// Saves search tokens for a page.
        /// </summary>
        void SavePageSearchTokens(List<EnginePageToken> tokens);

        /// <summary>
        /// Updates page references (outgoing links).
        /// </summary>
        void UpdatePageReferences(int pageId, List<PageReference> references);

        #endregion

        #region User Operations

        /// <summary>
        /// Gets public user profiles with pagination.
        /// </summary>
        List<EngineAccountProfile> GetPublicProfilesPaged(int pageNumber, int? pageSize = null, string? searchToken = null);

        #endregion

        #region File Attachments

        /// <summary>
        /// Gets file attachments for a page with pagination.
        /// </summary>
        List<EngineFileAttachment> GetPageFilesPaged(string pageNavigation, int pageNumber, int? pageSize = null, int? pageRevision = null);


        /// <summary>
        /// Gets a specific file attachment by navigation.
        /// </summary>
        EngineFileAttachment? GetFileAttachment(string pageNavigation, string fileNavigation, int? pageRevision = null);

        #endregion

        #region Configuration

        /// <summary>
        /// Gets configuration entries by group name.
        /// </summary>
        ConfigurationEntries GetConfigurationEntriesByGroupName(string groupName);

        #endregion

        #region Logging

        /// <summary>
        /// Logs an exception.
        /// </summary>
        void LogException(Exception ex, string? customText = null);

        /// <summary>
        /// Logs a custom error message.
        /// </summary>
        void LogException(string message);

        /// <summary>
        /// Records compilation statistics.
        /// </summary>
        void RecordCompilationStatistics(int pageId, double processingTimeMs, int matchCount, int errorCount,
            int outgoingLinkCount, int tagCount, int htmlSize, int bodySize);

        #endregion
    }
}
