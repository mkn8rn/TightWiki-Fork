using TightWiki.Contracts.DataModels;
using TightWiki.Web.Engine.Library;
using TightWiki.Utils;

namespace BLL.Services.Pages
{
    /// <summary>
    /// Service interface for page operations.
    /// </summary>
    public interface IPageService
    {
        // AutoComplete
        IEnumerable<Page> AutoCompletePage(string? searchText);
        IEnumerable<string> AutoCompleteNamespace(string? searchText);

        // Page Info Retrieval
        Page? GetPageRevisionInfoById(int pageId, int? revision = null);
        ProcessingInstructionCollection GetPageProcessingInstructionsByPageId(int pageId);
        List<PageTag> GetPageTagsById(int pageId);
        List<PageRevision> GetPageRevisionsInfoByNavigationPaged(string navigation, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);
        List<PageRevision> GetTopRecentlyModifiedPagesInfoByUserId(Guid userId, int topCount);
        string? GetPageNavigationByPageId(int pageId);
        List<Page> GetTopRecentlyModifiedPagesInfo(int topCount);

        // Page Search
        List<Page> PageSearch(List<string> searchTerms);
        List<Page> PageSearchPaged(List<string> searchTerms, int pageNumber, int? pageSize = null, bool? allowFuzzyMatching = null);
        List<RelatedPage> GetSimilarPagesPaged(int pageId, int similarity, int pageNumber, int? pageSize = null);
        List<RelatedPage> GetRelatedPagesPaged(int pageId, int pageNumber, int? pageSize = null);

        // Comments
        void InsertPageComment(int pageId, Guid userId, string body);
        void DeletePageCommentById(int pageId, int commentId);
        void DeletePageCommentByUserAndId(int pageId, Guid userId, int commentId);
        List<PageComment> GetPageCommentsPaged(string navigation, int pageNumber);

        // Missing/Nonexistent Pages
        List<NonexistentPage> GetMissingPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null);

        // Page References
        void UpdateSinglePageReference(string pageNavigation, int pageId);
        void UpdatePageReferences(int pageId, List<PageReference> referencesPageNavigations);

        // Page Listings
        List<Page> GetAllPagesByInstructionPaged(int pageNumber, string? instruction = null);
        List<int> GetDeletedPageIdsByTokens(List<string>? tokens);
        List<int> GetPageIdsByTokens(List<string>? tokens);
        List<Page> GetAllNamespacePagesPaged(int pageNumber, string namespaceName, string? orderBy = null, string? orderByDirection = null);
        List<Page> GetAllPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null);
        List<Page> GetAllDeletedPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null);
        List<NamespaceStat> GetAllNamespacesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null);
        List<string> GetAllNamespaces();
        List<Page> GetAllPages();
        List<Page> GetAllTemplatePages();
        List<FeatureTemplate> GetAllFeatureTemplates();

        // Processing Instructions
        void UpdatePageProcessingInstructions(int pageId, List<string> instructions);

        // Page CRUD
        Page? GetPageRevisionById(int pageId, int? revision = null);
        void SavePageSearchTokens(List<PageToken> items);
        void TruncateAllPageRevisions(string confirm);
        int GetCurrentPageRevision(int pageId);
        Page? GetLimitedPageInfoByIdAndRevision(int pageId, int? revision = null);
        int SavePage(Page page);
        Page? GetPageInfoByNavigation(string navigation);
        int GetPageRevisionCountByPageId(int pageId);

        // Deleted Pages
        void RestoreDeletedPageByPageId(int pageId);
        void MovePageRevisionToDeletedById(int pageId, int revision, Guid userId);
        void MovePageToDeletedById(int pageId, Guid userId);
        void PurgeDeletedPageByPageId(int pageId);
        void PurgeDeletedPages();
        int GetCountOfPageAttachmentsById(int pageId);
        Page? GetDeletedPageById(int pageId);
        Page? GetLatestPageRevisionById(int pageId);
        int GetPageNextRevision(int pageId, int revision);
        int GetPagePreviousRevision(int pageId, int revision);

        // Deleted Page Revisions
        List<DeletedPageRevision> GetDeletedPageRevisionsByIdPaged(int pageId, int pageNumber, string? orderBy = null, string? orderByDirection = null);
        void PurgeDeletedPageRevisions();
        void PurgeDeletedPageRevisionsByPageId(int pageId);
        void PurgeDeletedPageRevisionByPageIdAndRevision(int pageId, int revision);
        void RestoreDeletedPageRevisionByPageIdAndRevision(int pageId, int revision);
        DeletedPageRevision? GetDeletedPageRevisionById(int pageId, int revision);

        // Page Revision by Navigation
        Page? GetPageRevisionByNavigation(NamespaceNavigation navigation, int? revision = null);
        Page? GetPageRevisionByNavigation(string givenNavigation, int? revision = null, bool refreshCache = false);

        // Tags
        List<TagAssociation> GetAssociatedTags(string tag);
        List<Page> GetPageInfoByNamespaces(List<string> namespaces);
        List<Page> GetPageInfoByTags(List<string> tags);
        List<Page> GetPageInfoByTag(string tag);
        void UpdatePageTags(int pageId, List<string> tags);

        // Cache Management
        void FlushPageCache(int pageId);
    }
}
