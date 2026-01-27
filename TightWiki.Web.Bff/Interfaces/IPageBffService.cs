using TightWiki.Contracts.DTOs;
using TightWiki.Contracts.Requests;
using TightWiki.Contracts.Responses;

namespace TightWiki.Web.Bff.Interfaces
{
    /// <summary>
    /// BFF service for page operations.
    /// Orchestrates between Application (data) and Engine (rendering).
    /// </summary>
    public interface IPageBffService
    {
        /// <summary>
        /// Gets a page with pre-rendered HTML for display.
        /// Flow: Application ? Data ? BFF ? Engine ? Rendered HTML
        /// </summary>
        Task<PageRenderedDto?> GetPageRenderedAsync(string navigation, int? revision = null);

        /// <summary>
        /// Gets a page for editing (raw markup, no rendering).
        /// Flow: Application ? Data ? BFF ? DTO
        /// </summary>
        Task<PageEditDto?> GetPageForEditAsync(string navigation);

        /// <summary>
        /// Saves a page.
        /// Flow: BFF ? Application ? Database
        /// </summary>
        Task<ApiResponse<PageDto>> SavePageAsync(string navigation, SavePageRequest request, Guid userId);

        /// <summary>
        /// Deletes a page.
        /// </summary>
        Task<ApiResponse> DeletePageAsync(int pageId, Guid userId);

        /// <summary>
        /// Gets page revisions.
        /// </summary>
        Task<PaginatedResponse<PageDto>> GetPageRevisionsAsync(string navigation, int pageNumber, int pageSize);

        /// <summary>
        /// Searches pages with rendered snippets.
        /// </summary>
        Task<PaginatedResponse<SearchResultDto>> SearchPagesAsync(SearchRequest request);
    }
}
