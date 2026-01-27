using TightWiki.Contracts;
using TightWiki.Contracts.DataModels;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Statistics
{
    /// <summary>
    /// Business logic service for compilation statistics operations.
    /// </summary>
    public sealed class StatisticsService : IStatisticsService
    {
        private readonly WikiDbContext _db;

        public StatisticsService(WikiDbContext db)
        {
            _db = db;
        }

        public void RecordCompilation(
            int pageId,
            double wikifyTimeMs,
            int matchCount,
            int errorCount,
            int outgoingLinkCount,
            int tagCount,
            int processedBodySize,
            int bodySize)
        {
            var entity = new CompilationStatisticsEntityDB
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

            _db.CompilationStatistics.Add(entity);
            _db.SaveChanges();
        }

        public List<PageCompilationStatistics> GetCompilationStatisticsPaged(
            int pageNumber,
            string? orderBy = null,
            string? orderByDirection = null,
            int? pageSize = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize.Value;

            var statsQuery = _db.CompilationStatistics.AsNoTracking();

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
            var ordered = ApplyOrdering(groupedStats, orderBy, isAsc);

            // Apply pagination and map to result
            return ordered
                .Skip(skip)
                .Take(pageSize.Value)
                .Select(x => new PageCompilationStatistics
                {
                    // Note: Name, Namespace, Navigation will be empty until Page entity is migrated
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
        }

        public void PurgeCompilationStatistics()
        {
            _db.CompilationStatistics.ExecuteDelete();
        }

        #region Private Methods

        private static IEnumerable<T> ApplyOrdering<T>(
            List<T> items,
            string? orderBy,
            bool isAsc) where T : class
        {
            // Use dynamic ordering based on property name
            return (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "compilations" => isAsc
                    ? items.OrderBy(x => GetPropertyValue(x, "Compilations"))
                    : items.OrderByDescending(x => GetPropertyValue(x, "Compilations")),
                "avgbuildtimems" => isAsc
                    ? items.OrderBy(x => GetPropertyValue(x, "AvgBuildTimeMs"))
                    : items.OrderByDescending(x => GetPropertyValue(x, "AvgBuildTimeMs")),
                "avgwikimatches" => isAsc
                    ? items.OrderBy(x => GetPropertyValue(x, "AvgWikiMatches"))
                    : items.OrderByDescending(x => GetPropertyValue(x, "AvgWikiMatches")),
                "totalerrorcount" => isAsc
                    ? items.OrderBy(x => GetPropertyValue(x, "TotalErrorCount"))
                    : items.OrderByDescending(x => GetPropertyValue(x, "TotalErrorCount")),
                "latestbuild" or "createddate" => isAsc
                    ? items.OrderBy(x => GetPropertyValue(x, "LatestBuild"))
                    : items.OrderByDescending(x => GetPropertyValue(x, "LatestBuild")),
                _ => items.OrderByDescending(x => GetPropertyValue(x, "LatestBuild"))
            };
        }

        private static object? GetPropertyValue<T>(T obj, string propertyName)
        {
            return typeof(T).GetProperty(propertyName)?.GetValue(obj);
        }

        #endregion
    }
}

