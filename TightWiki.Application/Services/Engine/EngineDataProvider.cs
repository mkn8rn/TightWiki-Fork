using BLL.Services.Configuration;
using BLL.Services.Exception;
using BLL.Services.Pages;
using BLL.Services.PageFile;
using BLL.Services.Statistics;
using BLL.Services.Users;
using TightWiki.Contracts.DataModels;
using TightWiki.Web.Engine.Library;
using TightWiki.Web.Engine.Library.Interfaces;

namespace BLL.Services.Engine
{
    /// <summary>
    /// Adapter that bridges the Application/BLL layer to the Engine layer.
    /// Uses TightWiki.Contracts.DataModels types for data transfer.
    /// </summary>
    public sealed class EngineDataProvider : IEngineDataProvider
    {
        private readonly IPageService _pageService;
        private readonly IPageFileService _pageFileService;
        private readonly IUsersService _usersService;
        private readonly IConfigurationService _configurationService;
        private readonly IExceptionService _exceptionService;
        private readonly IStatisticsService _statisticsService;

        public EngineDataProvider(
            IPageService pageService,
            IPageFileService pageFileService,
            IUsersService usersService,
            IConfigurationService configurationService,
            IExceptionService exceptionService,
            IStatisticsService statisticsService)
        {
            _pageService = pageService;
            _pageFileService = pageFileService;
            _usersService = usersService;
            _configurationService = configurationService;
            _exceptionService = exceptionService;
            _statisticsService = statisticsService;
        }

        #region Page Operations

        public EnginePage? GetPageByNavigation(string navigation, int? revision = null)
        {
            var page = _pageService.GetPageRevisionByNavigation(navigation, revision);
            return page == null ? null : MapToEnginePage(page);
        }

        public List<EnginePage> GetTopRecentlyModifiedPages(int count)
            => _pageService.GetTopRecentlyModifiedPagesInfo(count).Select(MapToEnginePage).ToList();

        public List<EnginePage> GetPagesByNamespaces(List<string> namespaces)
            => _pageService.GetPageInfoByNamespaces(namespaces).Select(MapToEnginePage).ToList();

        public List<EnginePage> GetPagesByTags(List<string> tags)
            => _pageService.GetPageInfoByTags(tags).Select(MapToEnginePage).ToList();

        public List<EnginePage> SearchPages(List<string> searchTerms)
            => _pageService.PageSearch(searchTerms).Select(MapToEnginePage).ToList();

        public List<EnginePage> SearchPagesPaged(List<string> searchTerms, int pageNumber, int? pageSize = null, bool? allowFuzzyMatching = null)
            => _pageService.PageSearchPaged(searchTerms, pageNumber, pageSize, allowFuzzyMatching).Select(MapToEnginePage).ToList();

        public List<EngineRelatedPage> GetSimilarPagesPaged(int pageId, int similarity, int pageNumber, int? pageSize = null)
            => _pageService.GetSimilarPagesPaged(pageId, similarity, pageNumber, pageSize).Select(MapToEngineRelatedPage).ToList();

        public List<EngineRelatedPage> GetRelatedPagesPaged(int pageId, int pageNumber, int? pageSize = null)
            => _pageService.GetRelatedPagesPaged(pageId, pageNumber, pageSize).Select(MapToEngineRelatedPage).ToList();

        public List<EnginePageRevision> GetPageRevisionsInfoByNavigationPaged(string navigation, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
            => _pageService.GetPageRevisionsInfoByNavigationPaged(navigation, pageNumber, orderBy, orderByDirection, pageSize).Select(MapToEnginePageRevision).ToList();

        public List<EngineTagAssociation> GetAssociatedTags(string seedTag)
            => _pageService.GetAssociatedTags(seedTag).Select(t => new EngineTagAssociation { Tag = t.Tag, PageCount = t.PageCount }).ToList();

        #endregion

        #region Page Persistence

        public int SavePage(EnginePage page)
        {
            var modelPage = MapFromEnginePage(page);
            return _pageService.SavePage(modelPage);
        }

        public void UpdateSinglePageReference(string pageNavigation, int pageId)
            => _pageService.UpdateSinglePageReference(pageNavigation, pageId);

        public void UpdatePageTags(int pageId, List<string> tags)
            => _pageService.UpdatePageTags(pageId, tags);

        public void UpdatePageProcessingInstructions(int pageId, List<string> instructions)
            => _pageService.UpdatePageProcessingInstructions(pageId, instructions);

        public void SavePageSearchTokens(List<EnginePageToken> tokens)
        {
            var pageTokens = tokens.Select(t => new PageToken
            {
                PageId = t.PageId,
                Token = t.Token,
                DoubleMetaphone = t.DoubleMetaphone,
                Weight = t.Weight
            }).ToList();
            _pageService.SavePageSearchTokens(pageTokens);
        }

        public void UpdatePageReferences(int pageId, List<PageReference> references)
            => _pageService.UpdatePageReferences(pageId, references);

        #endregion

        #region User Operations

        public List<EngineAccountProfile> GetPublicProfilesPaged(int pageNumber, int? pageSize = null, string? searchToken = null)
            => _usersService.GetAllPublicProfilesPaged(pageNumber, pageSize, searchToken).Select(MapToEngineAccountProfile).ToList();

        #endregion

        #region File Attachments

        public List<EngineFileAttachment> GetPageFilesPaged(string pageNavigation, int pageNumber, int? pageSize = null, int? pageRevision = null)
            => _pageFileService.GetPageFilesInfoByPageNavigationAndPageRevisionPaged(pageNavigation, pageNumber, pageSize, pageRevision).Select(MapToEngineFileAttachment).ToList();

        public EngineFileAttachment? GetFileAttachment(string pageNavigation, string fileNavigation, int? pageRevision = null)
        {
            var attachment = _pageFileService.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(pageNavigation, fileNavigation, pageRevision);
            return attachment == null ? null : MapToEngineFileAttachment(attachment);
        }

        #endregion

        #region Configuration

        public ConfigurationEntries GetConfigurationEntriesByGroupName(string groupName)
        {
            var modelEntries = _configurationService.GetConfigurationEntriesByGroupName(groupName);
            return new ConfigurationEntries(modelEntries.Collection.Select(e => new ConfigurationEntry
            {
                Id = e.Id,
                ConfigurationGroupId = e.ConfigurationGroupId,
                Name = e.Name,
                Value = e.Value,
                DataTypeId = e.DataTypeId,
                Description = e.Description,
                IsEncrypted = e.IsEncrypted,
                DataType = e.DataType
            }).ToList());
        }

        #endregion

        #region Logging

        public void LogException(System.Exception ex, string? customText = null)
        {
            if (customText != null)
            {
                _exceptionService.LogException(ex, customText);
            }
            else
            {
                _exceptionService.LogException(ex);
            }
        }


        public void LogException(string message)
            => _exceptionService.LogException(message);

        public void RecordCompilationStatistics(int pageId, double processingTimeMs, int matchCount, int errorCount,
            int outgoingLinkCount, int tagCount, int htmlSize, int bodySize)
            => _statisticsService.RecordCompilation(pageId, processingTimeMs, matchCount, errorCount,
                outgoingLinkCount, tagCount, htmlSize, bodySize);

        #endregion

        #region Mapping: Contracts DataModels ? Engine Types

        private static EnginePage MapToEnginePage(Page page) => new()
        {
            Id = page.Id,
            Revision = page.Revision,
            MostCurrentRevision = page.MostCurrentRevision,
            Name = page.Name,
            Navigation = page.Navigation,
            Description = page.Description,
            Body = page.Body,
            CreatedDate = page.CreatedDate,
            ModifiedDate = page.ModifiedDate,
            Match = page.Match,
            Weight = page.Weight,
            Score = page.Score,
            PaginationPageCount = page.PaginationPageCount
        };

        private static Page MapFromEnginePage(EnginePage page) => new()
        {
            Id = page.Id,
            Revision = page.Revision,
            MostCurrentRevision = page.MostCurrentRevision,
            Name = page.Name,
            Navigation = page.Navigation,
            Description = page.Description,
            Body = page.Body,
            CreatedDate = page.CreatedDate,
            ModifiedDate = page.ModifiedDate
        };

        private static EngineRelatedPage MapToEngineRelatedPage(RelatedPage page) => new()
        {
            Id = page.Id,
            Name = page.Name,
            Navigation = page.Navigation,
            Description = page.Description,
            Matches = page.Matches,
            PaginationPageCount = page.PaginationPageCount,
            PaginationPageSize = page.PaginationPageSize
        };

        private static EnginePageRevision MapToEnginePageRevision(PageRevision rev) => new()
        {
            PageId = rev.PageId,
            Revision = rev.Revision,
            HighestRevision = rev.HighestRevision,
            HigherRevisionCount = rev.HigherRevisionCount,
            Name = rev.Name,
            Navigation = rev.Navigation,
            Description = rev.Description,
            ChangeSummary = rev.ChangeSummary,
            ChangeAnalysis = rev.ChangeAnalysis,
            ModifiedByUserId = rev.ModifiedByUserId,
            ModifiedByUserName = rev.ModifiedByUserName,
            ModifiedDate = rev.ModifiedDate,
            CreatedByUserId = rev.CreatedByUserId,
            CreatedByUserName = rev.CreatedByUserName,
            CreatedDate = rev.CreatedDate,
            PaginationPageCount = rev.PaginationPageCount,
            PaginationPageSize = rev.PaginationPageSize
        };

        private static EngineAccountProfile MapToEngineAccountProfile(AccountProfile profile) => new()
        {
            UserId = profile.UserId,
            EmailAddress = profile.EmailAddress,
            AccountName = profile.AccountName,
            Navigation = profile.Navigation,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            TimeZone = profile.TimeZone,
            Country = profile.Country,
            Language = profile.Language,
            Theme = profile.Theme,
            Biography = profile.Biography,
            CreatedDate = profile.CreatedDate,
            ModifiedDate = profile.ModifiedDate,
            PaginationPageCount = profile.PaginationPageCount,
            PaginationPageSize = profile.PaginationPageSize
        };

        private static EngineFileAttachment MapToEngineFileAttachment(PageFileAttachmentInfo attachment) => new()
        {
            Id = attachment.Id,
            PageId = attachment.PageId,
            Name = attachment.Name,
            FileNavigation = attachment.FileNavigation,
            PageNavigation = attachment.PageNavigation,
            ContentType = attachment.ContentType,
            Size = attachment.Size,
            FileRevision = attachment.FileRevision,
            CreatedDate = attachment.CreatedDate,
            CreatedByUserId = attachment.CreatedByUserId,
            CreatedByUserName = attachment.CreatedByUserName,
            CreatedByNavigation = attachment.CreatedByNavigation,
            PaginationPageCount = attachment.PaginationPageCount,
            PaginationPageSize = attachment.PaginationPageSize
        };

        #endregion
    }
}
