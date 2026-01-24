using DAL;
using DuoVia.FuzzyStrings;
using Microsoft.EntityFrameworkCore;
using NTDLS.Helpers;
using TightWiki.Caching;
using TightWiki.Engine.Library;
using TightWiki.Library;
using TightWiki.Models;
using DalPageEntity = DAL.Models.PageEntity;
using DalPageRevisionEntity = DAL.Models.PageRevisionEntity;
using DalPageTagEntity = DAL.Models.PageTagEntity;
using DalProcessingInstructionEntity = DAL.Models.ProcessingInstructionEntity;
using DalPageCommentEntity = DAL.Models.PageCommentEntity;
using DalPageReferenceEntity = DAL.Models.PageReferenceEntity;
using DalPageTokenEntity = DAL.Models.PageTokenEntity;
using DalDeletedPageEntity = DAL.Models.DeletedPageEntity;
using DalDeletedPageRevisionEntity = DAL.Models.DeletedPageRevisionEntity;
using ApiPage = TightWiki.Models.DataModels.Page;
using ApiPageRevision = TightWiki.Models.DataModels.PageRevision;
using ApiPageTag = TightWiki.Models.DataModels.PageTag;
using ApiPageToken = TightWiki.Models.DataModels.PageToken;
using ApiPageComment = TightWiki.Models.DataModels.PageComment;
using ApiPageSearchToken = TightWiki.Models.DataModels.PageSearchToken;
using ApiProcessingInstruction = TightWiki.Models.DataModels.ProcessingInstruction;
using ApiProcessingInstructionCollection = TightWiki.Models.DataModels.ProcessingInstructionCollection;
using ApiRelatedPage = TightWiki.Models.DataModels.RelatedPage;
using ApiNonexistentPage = TightWiki.Models.DataModels.NonexistentPage;
using ApiNamespaceStat = TightWiki.Models.DataModels.NamespaceStat;
using ApiFeatureTemplate = TightWiki.Models.DataModels.FeatureTemplate;
using ApiDeletedPageRevision = TightWiki.Models.DataModels.DeletedPageRevision;
using ApiTagAssociation = TightWiki.Models.DataModels.TagAssociation;
using ApiPageReference = TightWiki.Engine.Library.PageReference;

namespace TightWiki.Repository
{
    public interface IPageRepository
    {
        IEnumerable<ApiPage> AutoCompletePage(string? searchText);
        IEnumerable<string> AutoCompleteNamespace(string? searchText);
        ApiPage? GetPageRevisionInfoById(int pageId, int? revision = null);
        ApiProcessingInstructionCollection GetPageProcessingInstructionsByPageId(int pageId);
        List<ApiPageTag> GetPageTagsById(int pageId);
        List<ApiPageRevision> GetPageRevisionsInfoByNavigationPaged(string navigation, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);
        List<ApiPageRevision> GetTopRecentlyModifiedPagesInfoByUserId(Guid userId, int topCount);
        string? GetPageNavigationByPageId(int pageId);
        List<ApiPage> GetTopRecentlyModifiedPagesInfo(int topCount);
        List<ApiPage> PageSearch(List<string> searchTerms);
        List<ApiPage> PageSearchPaged(List<string> searchTerms, int pageNumber, int? pageSize = null, bool? allowFuzzyMatching = null);
        List<ApiRelatedPage> GetSimilarPagesPaged(int pageId, int similarity, int pageNumber, int? pageSize = null);
        List<ApiRelatedPage> GetRelatedPagesPaged(int pageId, int pageNumber, int? pageSize = null);
        void InsertPageComment(int pageId, Guid userId, string body);
        void DeletePageCommentById(int pageId, int commentId);
        void DeletePageCommentByUserAndId(int pageId, Guid userId, int commentId);
        List<ApiPageComment> GetPageCommentsPaged(string navigation, int pageNumber);
        List<ApiNonexistentPage> GetMissingPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null);
        void UpdateSinglePageReference(string pageNavigation, int pageId);
        void UpdatePageReferences(int pageId, List<ApiPageReference> referencesPageNavigations);
        List<ApiPage> GetAllPagesByInstructionPaged(int pageNumber, string? instruction = null);
        List<int> GetDeletedPageIdsByTokens(List<string>? tokens);
        List<int> GetPageIdsByTokens(List<string>? tokens);
        List<ApiPage> GetAllNamespacePagesPaged(int pageNumber, string namespaceName, string? orderBy = null, string? orderByDirection = null);
        List<ApiPage> GetAllPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null);
        List<ApiPage> GetAllDeletedPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null);
        List<ApiNamespaceStat> GetAllNamespacesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null);
        List<string> GetAllNamespaces();
        List<ApiPage> GetAllPages();
        List<ApiPage> GetAllTemplatePages();
        List<ApiFeatureTemplate> GetAllFeatureTemplates();
        void UpdatePageProcessingInstructions(int pageId, List<string> instructions);
        ApiPage? GetPageRevisionById(int pageId, int? revision = null);
        void SavePageSearchTokens(List<ApiPageToken> items);
        void TruncateAllPageRevisions(string confirm);
        int GetCurrentPageRevision(int pageId);
        ApiPage? GetLimitedPageInfoByIdAndRevision(int pageId, int? revision = null);
        int SavePage(ApiPage page);
        ApiPage? GetPageInfoByNavigation(string navigation);
        int GetPageRevisionCountByPageId(int pageId);
        void RestoreDeletedPageByPageId(int pageId);
        void MovePageRevisionToDeletedById(int pageId, int revision, Guid userId);
        void MovePageToDeletedById(int pageId, Guid userId);
        void PurgeDeletedPageByPageId(int pageId);
        void PurgeDeletedPages();
        int GetCountOfPageAttachmentsById(int pageId);
        ApiPage? GetDeletedPageById(int pageId);
        ApiPage? GetLatestPageRevisionById(int pageId);
        int GetPageNextRevision(int pageId, int revision);
        int GetPagePreviousRevision(int pageId, int revision);
        List<ApiDeletedPageRevision> GetDeletedPageRevisionsByIdPaged(int pageId, int pageNumber, string? orderBy = null, string? orderByDirection = null);
        void PurgeDeletedPageRevisions();
        void PurgeDeletedPageRevisionsByPageId(int pageId);
        void PurgeDeletedPageRevisionByPageIdAndRevision(int pageId, int revision);
        void RestoreDeletedPageRevisionByPageIdAndRevision(int pageId, int revision);
        ApiDeletedPageRevision? GetDeletedPageRevisionById(int pageId, int revision);
        ApiPage? GetPageRevisionByNavigation(NamespaceNavigation navigation, int? revision = null);
        ApiPage? GetPageRevisionByNavigation(string givenNavigation, int? revision = null, bool refreshCache = false);
        List<ApiTagAssociation> GetAssociatedTags(string tag);
        List<ApiPage> GetPageInfoByNamespaces(List<string> namespaces);
        List<ApiPage> GetPageInfoByTags(List<string> tags);
        List<ApiPage> GetPageInfoByTag(string tag);
        void UpdatePageTags(int pageId, List<string> tags);
    }

    public static class PageRepository
    {
        private static IServiceProvider? _serviceProvider;
        public static void UseServiceProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        private static IPageRepository Repo =>
            _serviceProvider?.GetService(typeof(IPageRepository)) as IPageRepository
            ?? throw new InvalidOperationException("IPageRepository is not configured.");

        public static IEnumerable<ApiPage> AutoCompletePage(string? searchText)
            => Repo.AutoCompletePage(searchText);

        public static IEnumerable<string> AutoCompleteNamespace(string? searchText)
            => Repo.AutoCompleteNamespace(searchText);

        public static ApiPage? GetPageRevisionInfoById(int pageId, int? revision = null)
            => Repo.GetPageRevisionInfoById(pageId, revision);

        public static ApiProcessingInstructionCollection GetPageProcessingInstructionsByPageId(int pageId)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [pageId]);
            return WikiCache.AddOrGet(cacheKey, () => Repo.GetPageProcessingInstructionsByPageId(pageId)).EnsureNotNull();
        }

        public static List<ApiPageTag> GetPageTagsById(int pageId)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [pageId]);
            return WikiCache.AddOrGet(cacheKey, () => Repo.GetPageTagsById(pageId)).EnsureNotNull();
        }

        public static List<ApiPageRevision> GetPageRevisionsInfoByNavigationPaged(
            string navigation, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
            => Repo.GetPageRevisionsInfoByNavigationPaged(navigation, pageNumber, orderBy, orderByDirection, pageSize);

        public static List<ApiPageRevision> GetTopRecentlyModifiedPagesInfoByUserId(Guid userId, int topCount)
            => Repo.GetTopRecentlyModifiedPagesInfoByUserId(userId, topCount);

        public static string? GetPageNavigationByPageId(int pageId)
            => Repo.GetPageNavigationByPageId(pageId);

        public static List<ApiPage> GetTopRecentlyModifiedPagesInfo(int topCount)
            => Repo.GetTopRecentlyModifiedPagesInfo(topCount);

        public static List<ApiPage> PageSearch(List<string> searchTerms)
            => Repo.PageSearch(searchTerms);

        public static List<ApiPage> PageSearchPaged(List<string> searchTerms, int pageNumber, int? pageSize = null, bool? allowFuzzyMatching = null)
            => Repo.PageSearchPaged(searchTerms, pageNumber, pageSize, allowFuzzyMatching);

        public static List<ApiRelatedPage> GetSimilarPagesPaged(int pageId, int similarity, int pageNumber, int? pageSize = null)
            => Repo.GetSimilarPagesPaged(pageId, similarity, pageNumber, pageSize);

        public static List<ApiRelatedPage> GetRelatedPagesPaged(int pageId, int pageNumber, int? pageSize = null)
            => Repo.GetRelatedPagesPaged(pageId, pageNumber, pageSize);

        public static void FlushPageCache(int pageId)
        {
            var pageNavigation = GetPageNavigationByPageId(pageId);
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [pageNavigation]));
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [pageId]));
        }

        public static void InsertPageComment(int pageId, Guid userId, string body)
        {
            Repo.InsertPageComment(pageId, userId, body);
            FlushPageCache(pageId);
        }

        public static void DeletePageCommentById(int pageId, int commentId)
        {
            Repo.DeletePageCommentById(pageId, commentId);
            FlushPageCache(pageId);
        }

        public static void DeletePageCommentByUserAndId(int pageId, Guid userId, int commentId)
        {
            Repo.DeletePageCommentByUserAndId(pageId, userId, commentId);
            FlushPageCache(pageId);
        }

        public static List<ApiPageComment> GetPageCommentsPaged(string navigation, int pageNumber)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [navigation, pageNumber, GlobalConfiguration.PaginationSize]);
            return WikiCache.AddOrGet(cacheKey, () => Repo.GetPageCommentsPaged(navigation, pageNumber)).EnsureNotNull();
        }

        public static List<ApiNonexistentPage> GetMissingPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
            => Repo.GetMissingPagesPaged(pageNumber, orderBy, orderByDirection);

        public static void UpdateSinglePageReference(string pageNavigation, int pageId)
        {
            Repo.UpdateSinglePageReference(pageNavigation, pageId);
            FlushPageCache(pageId);
        }

        public static void UpdatePageReferences(int pageId, List<ApiPageReference> referencesPageNavigations)
        {
            Repo.UpdatePageReferences(pageId, referencesPageNavigations);
            FlushPageCache(pageId);
        }

        public static List<ApiPage> GetAllPagesByInstructionPaged(int pageNumber, string? instruction = null)
            => Repo.GetAllPagesByInstructionPaged(pageNumber, instruction);

        public static List<int> GetDeletedPageIdsByTokens(List<string>? tokens)
            => Repo.GetDeletedPageIdsByTokens(tokens);

        public static List<int> GetPageIdsByTokens(List<string>? tokens)
            => Repo.GetPageIdsByTokens(tokens);

        public static List<ApiPage> GetAllNamespacePagesPaged(int pageNumber, string namespaceName,
            string? orderBy = null, string? orderByDirection = null)
            => Repo.GetAllNamespacePagesPaged(pageNumber, namespaceName, orderBy, orderByDirection);

        public static List<ApiPage> GetAllPagesPaged(int pageNumber,
            string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null)
            => Repo.GetAllPagesPaged(pageNumber, orderBy, orderByDirection, searchTerms);

        public static List<ApiPage> GetAllDeletedPagesPaged(int pageNumber, string? orderBy = null,
            string? orderByDirection = null, List<string>? searchTerms = null)
            => Repo.GetAllDeletedPagesPaged(pageNumber, orderBy, orderByDirection, searchTerms);

        public static List<ApiNamespaceStat> GetAllNamespacesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
            => Repo.GetAllNamespacesPaged(pageNumber, orderBy, orderByDirection);

        public static List<string> GetAllNamespaces()
            => Repo.GetAllNamespaces();

        public static List<ApiPage> GetAllPages()
            => Repo.GetAllPages();

        public static List<ApiPage> GetAllTemplatePages()
            => Repo.GetAllTemplatePages();

        public static List<ApiFeatureTemplate> GetAllFeatureTemplates()
            => WikiCache.AddOrGet(WikiCacheKeyFunction.Build(WikiCache.Category.Configuration), () =>
                Repo.GetAllFeatureTemplates()).EnsureNotNull();

        public static void UpdatePageProcessingInstructions(int pageId, List<string> instructions)
        {
            Repo.UpdatePageProcessingInstructions(pageId, instructions);
            FlushPageCache(pageId);
        }

        public static ApiPage? GetPageRevisionById(int pageId, int? revision = null)
            => WikiCache.AddOrGet(WikiCacheKeyFunction.Build(WikiCache.Category.Page, [pageId, revision]), () =>
                Repo.GetPageRevisionById(pageId, revision));

        public static void SavePageSearchTokens(List<ApiPageToken> items)
            => Repo.SavePageSearchTokens(items);

        public static void TruncateAllPageRevisions(string confirm)
            => Repo.TruncateAllPageRevisions(confirm);

        public static int GetCurrentPageRevision(int pageId)
            => WikiCache.AddOrGet(WikiCacheKeyFunction.Build(WikiCache.Category.Page, [pageId]), () =>
                Repo.GetCurrentPageRevision(pageId));

        public static ApiPage? GetLimitedPageInfoByIdAndRevision(int pageId, int? revision = null)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [pageId, revision]);
            return WikiCache.AddOrGet(cacheKey, () => Repo.GetLimitedPageInfoByIdAndRevision(pageId, revision));
        }

        public static int SavePage(ApiPage page)
            => Repo.SavePage(page);

        public static ApiPage? GetPageInfoByNavigation(string navigation)
            => WikiCache.AddOrGet(WikiCacheKeyFunction.Build(WikiCache.Category.Page, [navigation]), () =>
                Repo.GetPageInfoByNavigation(navigation));

        public static int GetPageRevisionCountByPageId(int pageId)
            => WikiCache.AddOrGet(WikiCacheKeyFunction.Build(WikiCache.Category.Page, [pageId]), () =>
                Repo.GetPageRevisionCountByPageId(pageId));

        public static void RestoreDeletedPageByPageId(int pageId)
        {
            Repo.RestoreDeletedPageByPageId(pageId);
            FlushPageCache(pageId);
        }

        public static void MovePageRevisionToDeletedById(int pageId, int revision, Guid userId)
        {
            Repo.MovePageRevisionToDeletedById(pageId, revision, userId);
            FlushPageCache(pageId);
        }

        public static void MovePageToDeletedById(int pageId, Guid userId)
        {
            Repo.MovePageToDeletedById(pageId, userId);
            FlushPageCache(pageId);
        }

        public static void PurgeDeletedPageByPageId(int pageId)
        {
            Repo.PurgeDeletedPageByPageId(pageId);
            FlushPageCache(pageId);
        }

        public static void PurgeDeletedPages()
            => Repo.PurgeDeletedPages();

        public static int GetCountOfPageAttachmentsById(int pageId)
            => Repo.GetCountOfPageAttachmentsById(pageId);

        public static ApiPage? GetDeletedPageById(int pageId)
            => Repo.GetDeletedPageById(pageId);

        public static ApiPage? GetLatestPageRevisionById(int pageId)
            => Repo.GetLatestPageRevisionById(pageId);

        public static int GetPageNextRevision(int pageId, int revision)
            => Repo.GetPageNextRevision(pageId, revision);

        public static int GetPagePreviousRevision(int pageId, int revision)
            => Repo.GetPagePreviousRevision(pageId, revision);

        public static List<ApiDeletedPageRevision> GetDeletedPageRevisionsByIdPaged(int pageId, int pageNumber,
            string? orderBy = null, string? orderByDirection = null)
            => Repo.GetDeletedPageRevisionsByIdPaged(pageId, pageNumber, orderBy, orderByDirection);

        public static void PurgeDeletedPageRevisions()
            => Repo.PurgeDeletedPageRevisions();

        public static void PurgeDeletedPageRevisionsByPageId(int pageId)
        {
            Repo.PurgeDeletedPageRevisionsByPageId(pageId);
            FlushPageCache(pageId);
        }

        public static void PurgeDeletedPageRevisionByPageIdAndRevision(int pageId, int revision)
        {
            Repo.PurgeDeletedPageRevisionByPageIdAndRevision(pageId, revision);
            FlushPageCache(pageId);
        }

        public static void RestoreDeletedPageRevisionByPageIdAndRevision(int pageId, int revision)
        {
            Repo.RestoreDeletedPageRevisionByPageIdAndRevision(pageId, revision);
            FlushPageCache(pageId);
        }

        public static ApiDeletedPageRevision? GetDeletedPageRevisionById(int pageId, int revision)
            => Repo.GetDeletedPageRevisionById(pageId, revision);

        public static ApiPage? GetPageRevisionByNavigation(NamespaceNavigation navigation, int? revision = null)
            => Repo.GetPageRevisionByNavigation(navigation, revision);

        public static ApiPage? GetPageRevisionByNavigation(string givenNavigation, int? revision = null, bool refreshCache = false)
        {
            var navigation = new NamespaceNavigation(givenNavigation);
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [navigation.Canonical, revision]);

            if (refreshCache)
            {
                WikiCache.Remove(cacheKey);
            }

            return WikiCache.AddOrGet(cacheKey, () => Repo.GetPageRevisionByNavigation(givenNavigation, revision, false));
        }

        public static List<ApiTagAssociation> GetAssociatedTags(string tag)
            => Repo.GetAssociatedTags(tag);

        public static List<ApiPage> GetPageInfoByNamespaces(List<string> namespaces)
            => Repo.GetPageInfoByNamespaces(namespaces);

        public static List<ApiPage> GetPageInfoByTags(List<string> tags)
            => Repo.GetPageInfoByTags(tags);

        public static List<ApiPage> GetPageInfoByTag(string tag)
            => Repo.GetPageInfoByTag(tag);

        public static void UpdatePageTags(int pageId, List<string> tags)
            => Repo.UpdatePageTags(pageId, tags);
    }
}