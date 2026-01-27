using BLL.Services.Configuration;
using BLL.Services.Pages;
using TightWiki.Contracts.DTOs;
using TightWiki.Contracts.Requests;
using TightWiki.Contracts.Responses;
using TightWiki.Web.Engine.Library.Interfaces;
using TightWiki.Web.Bff.Interfaces;

namespace TightWiki.Web.Bff.Services
{
    /// <summary>
    /// BFF service that orchestrates between Application (data) and Engine (rendering).
    /// 
    /// Key principle: Application and Engine never interact directly.
    /// Data flows: Application ? BFF ? Engine
    /// </summary>
    public class PageBffService : IPageBffService
    {
        private readonly IPageService _pageService;
        private readonly ITightEngine _engine;
        private readonly IEngineConfigurationProvider _configProvider;
        private readonly IEngineDataProvider _dataProvider;

        public PageBffService(
            IPageService pageService,
            ITightEngine engine,
            IEngineConfigurationProvider configProvider,
            IEngineDataProvider dataProvider)
        {
            _pageService = pageService;
            _engine = engine;
            _configProvider = configProvider;
            _dataProvider = dataProvider;
        }

        /// <summary>
        /// Gets a page with pre-rendered HTML.
        /// 1. Fetch raw data from Application
        /// 2. Get engine configuration from Application
        /// 3. Pass data and config to Engine for transformation
        /// 4. Return rendered DTO
        /// </summary>
        public async Task<PageRenderedDto?> GetPageRenderedAsync(string navigation, int? revision = null)
        {
            // Step 1: Get data from Application layer
            var page = _pageService.GetPageRevisionByNavigation(navigation, revision);
            if (page == null)
                return null;

            // Step 2: Get engine configuration from Application layer
            var config = _configProvider.GetEngineConfiguration();

            // Step 3: Pass page data and config TO the engine for transformation
            // Note: Engine receives data from BFF, not from Application directly
            var engineState = _engine.Transform(config, _dataProvider, null, page, revision);

            // Step 4: Map to rendered DTO
            return new PageRenderedDto
            {
                Id = page.Id,
                Name = page.Name,
                Title = page.Title,
                Navigation = page.Navigation,
                Namespace = page.Namespace ?? string.Empty,
                Description = page.Description,
                RenderedBody = engineState.HtmlResult,
                Revision = page.Revision,
                MostCurrentRevision = page.MostCurrentRevision,
                ModifiedDate = page.ModifiedDate,
                ModifiedByUserName = page.ModifiedByUserName,
                TableOfContents = engineState.TableOfContents
                    .Select(t => new TableOfContentsItemDto
                    {
                        Level = t.Level,
                        Text = t.Text,
                        Anchor = t.HrefTag
                    }).ToList(),
                OutgoingLinks = engineState.OutgoingLinks.Select(l => l.Navigation).ToList(),
                ProcessingInstructions = engineState.ProcessingInstructions,
                Tags = engineState.Tags
            };
        }

        /// <summary>
        /// Gets page for editing - no Engine processing needed.
        /// </summary>
        public async Task<PageEditDto?> GetPageForEditAsync(string navigation)
        {
            var page = _pageService.GetPageRevisionByNavigation(navigation);
            if (page == null)
                return null;

            var tags = _pageService.GetPageTagsById(page.Id)?
                .Select(t => t.Tag).ToList() ?? [];

            return new PageEditDto
            {
                Id = page.Id,
                Name = page.Name,
                Navigation = page.Navigation,
                Namespace = page.Namespace ?? string.Empty,
                Description = page.Description,
                Body = page.Body,
                Tags = tags
            };
        }

        /// <summary>
        /// Saves a page - goes directly to Application layer.
        /// </summary>
        public async Task<ApiResponse<PageDto>> SavePageAsync(string navigation, SavePageRequest request, Guid userId)
        {
            try
            {
                // TODO: Implement save via Application layer
                // The Application layer handles all business rules and validation
                
                throw new NotImplementedException("SavePageAsync not yet implemented");
            }
            catch (Exception ex)
            {
                return ApiResponse<PageDto>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Deletes a page.
        /// </summary>
        public async Task<ApiResponse> DeletePageAsync(int pageId, Guid userId)
        {
            try
            {
                // TODO: Implement delete via Application layer
                throw new NotImplementedException("DeletePageAsync not yet implemented");
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Gets page revisions - no Engine processing needed.
        /// </summary>
        public async Task<PaginatedResponse<PageDto>> GetPageRevisionsAsync(string navigation, int pageNumber, int pageSize)
        {
            var revisions = _pageService.GetPageRevisionsInfoByNavigationPaged(
                navigation, pageNumber, null, null, pageSize);

            return new PaginatedResponse<PageDto>
            {
                Items = revisions.Select(r => new PageDto
                {
                    Id = r.PageId,
                    Name = r.Name,
                    Navigation = r.Navigation,
                    Revision = r.Revision,
                    ModifiedDate = r.ModifiedDate,
                    ModifiedByUserName = r.ModifiedByUserName
                }).ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = revisions.FirstOrDefault()?.PaginationPageCount ?? 0,
                TotalCount = revisions.Count
            };
        }

        /// <summary>
        /// Searches pages.
        /// </summary>
        public async Task<PaginatedResponse<SearchResultDto>> SearchPagesAsync(SearchRequest request)
        {
            var tokens = request.Query.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            
            var results = _pageService.PageSearchPaged(
                tokens, 
                request.PageNumber, 
                request.PageSize, 
                request.AllowFuzzyMatching);

            return new PaginatedResponse<SearchResultDto>
            {
                Items = results.Select(p => new SearchResultDto
                {
                    PageId = p.Id,
                    Name = p.Name,
                    Title = p.Title,
                    Navigation = p.Navigation,
                    Namespace = p.Namespace ?? string.Empty,
                    Description = p.Description,
                    Score = p.Score,
                    Match = p.Match,
                    ModifiedDate = p.ModifiedDate
                }).ToList(),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = results.FirstOrDefault()?.PaginationPageCount ?? 0,
                TotalCount = results.Count
            };
        }
    }
}
