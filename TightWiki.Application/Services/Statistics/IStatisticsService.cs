using TightWiki.Contracts.DataModels;

namespace BLL.Services.Statistics
{
    /// <summary>
    /// Service interface for compilation statistics operations.
    /// </summary>
    public interface IStatisticsService
    {
        /// <summary>
        /// Records compilation statistics for a page.
        /// </summary>
        void RecordCompilation(
            int pageId,
            double wikifyTimeMs,
            int matchCount,
            int errorCount,
            int outgoingLinkCount,
            int tagCount,
            int processedBodySize,
            int bodySize);

        /// <summary>
        /// Gets compilation statistics with pagination and sorting.
        /// </summary>
        List<PageCompilationStatistics> GetCompilationStatisticsPaged(
            int pageNumber,
            string? orderBy = null,
            string? orderByDirection = null,
            int? pageSize = null);

        /// <summary>
        /// Purges all compilation statistics.
        /// </summary>
        void PurgeCompilationStatistics();
    }
}
