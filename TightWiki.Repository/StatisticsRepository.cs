using DAL;
using Microsoft.EntityFrameworkCore;
using TightWiki.Models;
using TightWiki.Models.DataModels;
using DalCompilationStatisticsEntity = DAL.Models.CompilationStatisticsEntity;
using ApiPageCompilationStatistics = TightWiki.Models.DataModels.PageCompilationStatistics;

namespace TightWiki.Repository
{
    public interface IStatisticsRepository
    {
        void InsertCompilationStatistics(int pageId, double wikifyTimeMs, int matchCount, int errorCount,
            int outgoingLinkCount, int tagCount, int processedBodySize, int bodySize);
        void PurgeCompilationStatistics();
        List<ApiPageCompilationStatistics> GetCompilationStatisticsPaged(int pageNumber,
            string? orderBy = null, string? orderByDirection = null, int? pageSize = null);
    }

    public static class StatisticsRepository
    {
        private static IServiceProvider? _serviceProvider;
        public static void UseServiceProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        private static IStatisticsRepository Repo =>
            _serviceProvider?.GetService(typeof(IStatisticsRepository)) as IStatisticsRepository
            ?? throw new InvalidOperationException("IStatisticsRepository is not configured.");

        public static void InsertCompilationStatistics(int pageId,
            double wikifyTimeMs, int matchCount, int errorCount, int outgoingLinkCount,
            int tagCount, int processedBodySize, int bodySize)
            => Repo.InsertCompilationStatistics(pageId, wikifyTimeMs, matchCount, errorCount,
                outgoingLinkCount, tagCount, processedBodySize, bodySize);

        public static void PurgeCompilationStatistics()
            => Repo.PurgeCompilationStatistics();

        public static List<PageCompilationStatistics> GetCompilationStatisticsPaged(
            int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
            => Repo.GetCompilationStatisticsPaged(pageNumber, orderBy, orderByDirection, pageSize);
    }

    public sealed class StatisticsRepositoryEf : IStatisticsRepository
    {
        private readonly WikiDbContext _wikiDb;

        public StatisticsRepositoryEf(WikiDbContext wikiDb)
        {
            _wikiDb = wikiDb;
        }

        public void InsertCompilationStatistics(int pageId, double wikifyTimeMs, int matchCount, int errorCount,
            int outgoingLinkCount, int tagCount, int processedBodySize, int bodySize)
        {
            var entity = new DalCompilationStatisticsEntity
            {
                PageId = pageId,
                CreatedDate = DateTime.UtcNow,
                WikifyTimeMs = wikifyTimeMs,
                MatchCount = matchCount,
                ErrorCount = errorCount,
                OutgoingLinkCount = outgoingLinkCount,
                TagCount = tagCount,
                ProcessedBodySize = processedBodySize,
                BodySize = bodySize
            };

            _wikiDb.CompilationStatistics.Add(entity);
            _wikiDb.SaveChanges();
        }

        public void PurgeCompilationStatistics()
        {
            _wikiDb.CompilationStatistics.ExecuteDelete();
        }

        public List<ApiPageCompilationStatistics> GetCompilationStatisticsPaged(int pageNumber,
            string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize.Value;

            // Note: This query requires Page data which will be added when PageRepository is migrated.
            // For now, we'll query compilation statistics and join with pages when available.
            // The aggregation is done in-memory since we need to group by PageId.

            var statsQuery = _wikiDb.CompilationStatistics.AsNoTracking();

            // Get aggregated statistics grouped by PageId
            var groupedStats = statsQuery
                .GroupBy(s => s.PageId)
                .Select(g => new
                {
                    PageId = g.Key,
                    LatestBuild = g.Max(s => s.CreatedDate),
                    Compilations = g.Count(),
                    AvgBuildTimeMs = g.Average(s => s.WikifyTimeMs),
                    AvgWikiMatches = g.Average(s => s.MatchCount),
                    TotalErrorCount = g.Sum(s => s.ErrorCount),
                    AvgOutgoingLinkCount = g.Average(s => s.OutgoingLinkCount),
                    AvgTagCount = g.Average(s => s.TagCount),
                    AvgRawBodySize = g.Average(s => s.BodySize),
                    AvgWikifiedBodySize = g.Average(s => s.ProcessedBodySize)
                })
                .ToList();

            var totalCount = groupedStats.Count;
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

            // Apply ordering
            var isAsc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);
            var ordered = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "compilations" => isAsc
                    ? groupedStats.OrderBy(x => x.Compilations)
                    : groupedStats.OrderByDescending(x => x.Compilations),
                "avgbuildtimems" => isAsc
                    ? groupedStats.OrderBy(x => x.AvgBuildTimeMs)
                    : groupedStats.OrderByDescending(x => x.AvgBuildTimeMs),
                "avgwikimatches" => isAsc
                    ? groupedStats.OrderBy(x => x.AvgWikiMatches)
                    : groupedStats.OrderByDescending(x => x.AvgWikiMatches),
                "totalerrorcount" => isAsc
                    ? groupedStats.OrderBy(x => x.TotalErrorCount)
                    : groupedStats.OrderByDescending(x => x.TotalErrorCount),
                "latestbuild" or "createddate" => isAsc
                    ? groupedStats.OrderBy(x => x.LatestBuild)
                    : groupedStats.OrderByDescending(x => x.LatestBuild),
                _ => groupedStats.OrderByDescending(x => x.LatestBuild)
            };

            // Apply pagination and map to result
            var results = ordered
                .Skip(skip)
                .Take(pageSize.Value)
                .Select(x => new ApiPageCompilationStatistics
                {
                    // Note: Name, Namespace, Navigation will be empty until Page entity is migrated
                    // These should be populated by joining with Page table
                    Name = $"Page #{x.PageId}",
                    Namespace = string.Empty,
                    Navigation = string.Empty,
                    LatestBuild = x.LatestBuild,
                    Compilations = x.Compilations,
                    AvgBuildTimeMs = (decimal)x.AvgBuildTimeMs,
                    AvgWikiMatches = (decimal)x.AvgWikiMatches,
                    TotalErrorCount = x.TotalErrorCount,
                    AvgOutgoingLinkCount = (decimal)x.AvgOutgoingLinkCount,
                    AvgTagCount = (decimal)x.AvgTagCount,
                    AvgRawBodySize = (decimal)x.AvgRawBodySize,
                    AvgWikifiedBodySize = (decimal)x.AvgWikifiedBodySize,
                    PaginationPageSize = pageSize.Value,
                    PaginationPageCount = pageCount
                })
                .ToList();

            return results;
        }
    }
}

