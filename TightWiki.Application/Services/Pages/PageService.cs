using TightWiki.Contracts;
using TightWiki.Contracts.DataModels;
using DAL;
using DAL.Models;
using DuoVia.FuzzyStrings;
using Microsoft.EntityFrameworkCore;
using NTDLS.Helpers;
using TightWiki.Utils.Caching;
using TightWiki.Web.Engine.Library;
using TightWiki.Utils;

namespace BLL.Services.Pages
{
    /// <summary>
    /// Business logic service for page operations.
    /// Uses Entity Framework Core directly and manages caching.
    /// </summary>
    public sealed class PageService : IPageService
    {
        private readonly WikiDbContext _db;

        public PageService(WikiDbContext db)
        {
            _db = db;
        }

        #region Cache Management

        public void FlushPageCache(int pageId)
        {
            var pageNavigation = GetPageNavigationByPageId(pageId);
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [pageNavigation]));
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [pageId]));
        }

        #endregion

        #region AutoComplete

        public IEnumerable<Page> AutoCompletePage(string? searchText)
        {
            var text = searchText ?? string.Empty;
            return _db.Pages.AsNoTracking()
                .Where(p => EF.Functions.ILike(p.Name, $"%{text}%"))
                .OrderBy(p => p.Name)
                .Take(25)
                .Select(p => new Page
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation
                })
                .ToList();
        }

        public IEnumerable<string> AutoCompleteNamespace(string? searchText)
        {
            var text = searchText ?? string.Empty;
            return _db.Pages.AsNoTracking()
                .Where(p => EF.Functions.ILike(p.Namespace, $"%{text}%"))
                .Select(p => p.Namespace)
                .Distinct()
                .OrderBy(n => n)
                .Take(25)
                .ToList();
        }

        #endregion

        #region Page Info Retrieval

        public Page? GetPageRevisionInfoById(int pageId, int? revision = null)
        {
            var page = _db.Pages.AsNoTracking().SingleOrDefault(p => p.Id == pageId);
            if (page == null) return null;

            var effectiveRevision = revision ?? page.Revision;

            var pr = _db.PageRevisions.AsNoTracking()
                .SingleOrDefault(r => r.PageId == pageId && r.Revision == effectiveRevision);

            if (pr == null) return null;

            return MapToPage(page, pr, effectiveRevision);
        }

        public ProcessingInstructionCollection GetPageProcessingInstructionsByPageId(int pageId)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [pageId]);
            return WikiCache.AddOrGet(cacheKey, () =>
            {
                var instructions = _db.ProcessingInstructions.AsNoTracking()
                    .Where(pi => pi.PageId == pageId)
                    .Select(pi => new ProcessingInstruction { Instruction = pi.Instruction })
                    .ToList();

                return new ProcessingInstructionCollection { Collection = instructions };
            }).EnsureNotNull();
        }

        public List<PageTag> GetPageTagsById(int pageId)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [pageId]);
            return WikiCache.AddOrGet(cacheKey, () =>
                _db.PageTags.AsNoTracking()
                    .Where(t => t.PageId == pageId)
                    .Select(t => new PageTag
                    {
                        Id = t.Id,
                        PageId = t.PageId,
                        Tag = t.Tag
                    })
                    .ToList()
            ).EnsureNotNull();
        }

        public List<PageRevision> GetPageRevisionsInfoByNavigationPaged(
            string navigation, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize.Value;

            var page = _db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == navigation);
            if (page == null) return [];

            var query = from pr in _db.PageRevisions.AsNoTracking()
                        join profile in _db.Profiles.AsNoTracking() on pr.ModifiedByUserId equals profile.UserId into profiles
                        from profile in profiles.DefaultIfEmpty()
                        where pr.PageId == page.Id
                        select new { pr, profile };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

            var isAsc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);
            var orderedQuery = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "revision" => isAsc ? query.OrderBy(x => x.pr.Revision) : query.OrderByDescending(x => x.pr.Revision),
                "modifieddate" => isAsc ? query.OrderBy(x => x.pr.ModifiedDate) : query.OrderByDescending(x => x.pr.ModifiedDate),
                _ => query.OrderByDescending(x => x.pr.Revision)
            };

            return orderedQuery
                .Skip(skip)
                .Take(pageSize.Value)
                .Select(x => new PageRevision
                {
                    PageId = x.pr.PageId,
                    Revision = x.pr.Revision,
                    Name = x.pr.Name,
                    Description = x.pr.Description,
                    ModifiedByUserId = x.pr.ModifiedByUserId,
                    ModifiedByUserName = x.profile != null ? x.profile.AccountName : string.Empty,
                    ModifiedDate = x.pr.ModifiedDate,
                    ChangeSummary = x.pr.ChangeSummary,
                    Navigation = x.pr.Navigation,
                    HighestRevision = page.Revision,
                    PaginationPageCount = pageCount,
                    PaginationPageSize = pageSize.Value
                })
                .ToList();
        }

        public List<PageRevision> GetTopRecentlyModifiedPagesInfoByUserId(Guid userId, int topCount)
        {
            return (from pr in _db.PageRevisions.AsNoTracking()
                    join p in _db.Pages.AsNoTracking() on pr.PageId equals p.Id
                    where pr.ModifiedByUserId == userId
                    orderby pr.ModifiedDate descending
                    select new PageRevision
                    {
                        PageId = pr.PageId,
                        Revision = pr.Revision,
                        Name = pr.Name,
                        Description = pr.Description,
                        ModifiedByUserId = pr.ModifiedByUserId,
                        ModifiedDate = pr.ModifiedDate,
                        Navigation = pr.Navigation
                    })
                .Take(topCount)
                .ToList();
        }

        public string? GetPageNavigationByPageId(int pageId)
        {
            return _db.Pages.AsNoTracking()
                .Where(p => p.Id == pageId)
                .Select(p => p.Navigation)
                .SingleOrDefault();
        }

        public List<Page> GetTopRecentlyModifiedPagesInfo(int topCount)
        {
            return _db.Pages.AsNoTracking()
                .OrderByDescending(p => p.ModifiedDate)
                .Take(topCount)
                .Select(p => new Page
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    ModifiedDate = p.ModifiedDate
                })
                .ToList();
        }

        #endregion

        #region Page Search

        public List<Page> PageSearch(List<string> searchTerms)
        {
            if (searchTerms.Count == 0) return [];

            // OBSOLETE: Static ConfigurationRepository removed. Default to true.
            var allowFuzzyMatching = true; // ConfigurationRepository.Get<bool>("Search", "Allow Fuzzy Matching");
            var meteredSearchTokens = GetMeteredPageSearchTokens(searchTerms, allowFuzzyMatching);
            if (meteredSearchTokens.Count == 0) return [];

            var pageIds = meteredSearchTokens.Select(t => t.PageId).ToList();

            return (from p in _db.Pages.AsNoTracking()
                    join profile in _db.Profiles.AsNoTracking() on p.ModifiedByUserId equals profile.UserId into profiles
                    from profile in profiles.DefaultIfEmpty()
                    where pageIds.Contains(p.Id)
                    select new Page
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Navigation = p.Navigation,
                        Description = p.Description,
                        ModifiedDate = p.ModifiedDate,
                        ModifiedByUserName = profile != null ? profile.AccountName : string.Empty
                    })
                .ToList();
        }

        public List<Page> PageSearchPaged(List<string> searchTerms, int pageNumber, int? pageSize = null, bool? allowFuzzyMatching = null)
        {
            if (searchTerms.Count == 0) return [];

            pageSize ??= GlobalConfiguration.PaginationSize;
            // OBSOLETE: Static ConfigurationRepository removed. Default to true.
            allowFuzzyMatching ??= true; // ConfigurationRepository.Get<bool>("Search", "Allow Fuzzy Matching");

            var meteredSearchTokens = GetMeteredPageSearchTokens(searchTerms, allowFuzzyMatching.Value);
            if (meteredSearchTokens.Count == 0) return [];

            var skip = (pageNumber - 1) * pageSize.Value;
            var pageIds = meteredSearchTokens.Select(t => t.PageId).ToList();
            var tokenLookup = meteredSearchTokens.ToDictionary(t => t.PageId);

            var query = from p in _db.Pages.AsNoTracking()
                        join profile in _db.Profiles.AsNoTracking() on p.ModifiedByUserId equals profile.UserId into profiles
                        from profile in profiles.DefaultIfEmpty()
                        where pageIds.Contains(p.Id)
                        select new { p, profile };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

            var results = query
                .Skip(skip)
                .Take(pageSize.Value)
                .ToList();

            return results.Select(x =>
            {
                var token = tokenLookup.GetValueOrDefault(x.p.Id);
                return new Page
                {
                    Id = x.p.Id,
                    Name = x.p.Name,
                    Navigation = x.p.Navigation,
                    Description = x.p.Description,
                    ModifiedDate = x.p.ModifiedDate,
                    ModifiedByUserName = x.profile?.AccountName ?? string.Empty,
                    Score = (decimal)(token?.Score ?? 0),
                    Match = (decimal)(token?.Match ?? 0),
                    Weight = (decimal)(token?.Weight ?? 0),
                    PaginationPageCount = pageCount
                };
            }).ToList();
        }

        private List<PageSearchToken> GetMeteredPageSearchTokens(List<string> searchTerms, bool allowFuzzyMatching)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Search, [string.Join(',', searchTerms), allowFuzzyMatching]);

            return WikiCache.AddOrGet(cacheKey, () =>
            {
                // OBSOLETE: Static ConfigurationRepository removed. Using default minimum match score.
                var minimumMatchScore = 0.25f; // ConfigurationRepository.Get<float>("Search", "Minimum Match Score");

                var searchTokens = searchTerms.Select(t => new PageToken
                {
                    Token = t,
                    DoubleMetaphone = t.ToDoubleMetaphone()
                }).ToList();

                if (allowFuzzyMatching)
                {
                    var allTokens = GetExactPageSearchTokens(searchTokens, minimumMatchScore / 2.0);
                    var fuzzyTokens = GetFuzzyPageSearchTokens(searchTokens, minimumMatchScore / 2.0);
                    allTokens.AddRange(fuzzyTokens);

                    return allTokens
                        .GroupBy(t => t.PageId)
                        .Where(g => g.Sum(x => x.Score) >= minimumMatchScore)
                        .Select(g => new PageSearchToken
                        {
                            PageId = g.Key,
                            Match = g.Max(x => x.Match),
                            Weight = g.Max(x => x.Weight),
                            Score = g.Max(x => x.Score)
                        }).ToList();
                }

                return GetExactPageSearchTokens(searchTokens, minimumMatchScore / 2.0);
            }).EnsureNotNull();
        }

        private List<PageSearchToken> GetExactPageSearchTokens(List<PageToken> tokens, double minimumMatchScore)
        {
            var tokenStrings = tokens.Select(t => t.Token.ToLowerInvariant()).ToList();

            return _db.PageTokens.AsNoTracking()
                .Where(pt => tokenStrings.Contains(pt.Token.ToLower()))
                .GroupBy(pt => pt.PageId)
                .Select(g => new PageSearchToken
                {
                    PageId = g.Key,
                    Match = g.Count() / (double)tokens.Count,
                    Weight = g.Sum(x => x.Weight),
                    Score = (g.Count() / (double)tokens.Count) * g.Sum(x => x.Weight)
                })
                .Where(t => t.Score >= minimumMatchScore)
                .ToList();
        }

        private List<PageSearchToken> GetFuzzyPageSearchTokens(List<PageToken> tokens, double minimumMatchScore)
        {
            var metaphones = tokens.Select(t => t.DoubleMetaphone).Where(m => !string.IsNullOrEmpty(m)).ToList();

            return _db.PageTokens.AsNoTracking()
                .Where(pt => metaphones.Contains(pt.DoubleMetaphone))
                .GroupBy(pt => pt.PageId)
                .Select(g => new PageSearchToken
                {
                    PageId = g.Key,
                    Match = g.Count() / (double)tokens.Count * 0.8,
                    Weight = g.Sum(x => x.Weight) * 0.8,
                    Score = (g.Count() / (double)tokens.Count) * g.Sum(x => x.Weight) * 0.8
                })
                .Where(t => t.Score >= minimumMatchScore)
                .ToList();
        }

        public List<RelatedPage> GetSimilarPagesPaged(int pageId, int similarity, int pageNumber, int? pageSize = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize.Value;

            var pageTags = _db.PageTags.AsNoTracking()
                .Where(t => t.PageId == pageId)
                .Select(t => t.Tag)
                .ToList();

            if (pageTags.Count == 0) return [];

            var query = from pt in _db.PageTags.AsNoTracking()
                        join p in _db.Pages.AsNoTracking() on pt.PageId equals p.Id
                        where pt.PageId != pageId && pageTags.Contains(pt.Tag)
                        group p by new { p.Id, p.Name, p.Navigation } into g
                        where g.Count() >= similarity
                        select new RelatedPage
                        {
                            Id = g.Key.Id,
                            Name = g.Key.Name,
                            Navigation = g.Key.Navigation,
                            Matches = g.Count()
                        };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

            return query
                .OrderByDescending(r => r.Matches)
                .Skip(skip)
                .Take(pageSize.Value)
                .ToList()
                .Select(r => { r.PaginationPageCount = pageCount; return r; })
                .ToList();
        }

        public List<RelatedPage> GetRelatedPagesPaged(int pageId, int pageNumber, int? pageSize = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize.Value;

            var query = from pr in _db.PageReferences.AsNoTracking()
                        join p in _db.Pages.AsNoTracking() on pr.ReferencesPageId equals p.Id
                        where pr.PageId == pageId && pr.ReferencesPageId != null
                        select new RelatedPage
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Navigation = p.Navigation
                        };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

            return query
                .OrderBy(r => r.Name)
                .Skip(skip)
                .Take(pageSize.Value)
                .ToList()
                .Select(r => { r.PaginationPageCount = pageCount; return r; })
                .ToList();
        }

        #endregion

        #region Comments

        public void InsertPageComment(int pageId, Guid userId, string body)
        {
            var entity = new PageCommentEntityDB
            {
                PageId = pageId,
                UserId = userId,
                Body = body,
                CreatedDate = DateTime.UtcNow
            };

            _db.PageComments.Add(entity);
            _db.SaveChanges();
            FlushPageCache(pageId);
        }

        public void DeletePageCommentById(int pageId, int commentId)
        {
            var entity = _db.PageComments.SingleOrDefault(c => c.Id == commentId && c.PageId == pageId);
            if (entity != null)
            {
                _db.PageComments.Remove(entity);
                _db.SaveChanges();
            }
            FlushPageCache(pageId);
        }

        public void DeletePageCommentByUserAndId(int pageId, Guid userId, int commentId)
        {
            var entity = _db.PageComments.SingleOrDefault(c => c.Id == commentId && c.PageId == pageId && c.UserId == userId);
            if (entity != null)
            {
                _db.PageComments.Remove(entity);
                _db.SaveChanges();
            }
            FlushPageCache(pageId);
        }

        public List<PageComment> GetPageCommentsPaged(string navigation, int pageNumber)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [navigation, pageNumber, GlobalConfiguration.PaginationSize]);
            return WikiCache.AddOrGet(cacheKey, () =>
            {
                var pageSize = GlobalConfiguration.PaginationSize;
                var skip = (pageNumber - 1) * pageSize;

                var page = _db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == navigation);
                if (page == null) return [];

                var query = from c in _db.PageComments.AsNoTracking()
                            join profile in _db.Profiles.AsNoTracking() on c.UserId equals profile.UserId into profiles
                            from profile in profiles.DefaultIfEmpty()
                            where c.PageId == page.Id
                            select new { c, profile };

                var totalCount = query.Count();
                var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

                return query
                    .OrderByDescending(x => x.c.CreatedDate)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(x => new PageComment
                    {
                        Id = x.c.Id,
                        PageId = x.c.PageId,
                        UserId = x.c.UserId,
                        Body = x.c.Body,
                        CreatedDate = x.c.CreatedDate,
                        UserName = x.profile != null ? x.profile.AccountName : string.Empty,
                        UserNavigation = x.profile != null ? x.profile.Navigation : string.Empty,
                        PageName = page.Name,
                        PaginationPageCount = pageCount
                    })
                    .ToList();
            }).EnsureNotNull();
        }

        #endregion

        #region Missing/Nonexistent Pages

        public List<NonexistentPage> GetMissingPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            var query = from pr in _db.PageReferences.AsNoTracking()
                        join p in _db.Pages.AsNoTracking() on pr.PageId equals p.Id
                        where pr.ReferencesPageId == null
                        select new { pr, p };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

            var isAsc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);
            var orderedQuery = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "sourcepagename" => isAsc ? query.OrderBy(x => x.p.Name) : query.OrderByDescending(x => x.p.Name),
                "targetpagenavigation" => isAsc ? query.OrderBy(x => x.pr.ReferencesPageNavigation) : query.OrderByDescending(x => x.pr.ReferencesPageNavigation),
                _ => query.OrderBy(x => x.pr.ReferencesPageNavigation)
            };

            return orderedQuery
                .Skip(skip)
                .Take(pageSize)
                .Select(x => new NonexistentPage
                {
                    SourcePageId = x.p.Id,
                    SourcePageName = x.p.Name,
                    SourcePageNavigation = x.p.Navigation,
                    TargetPageNavigation = x.pr.ReferencesPageNavigation,
                    TargetPageName = x.pr.ReferencesPageNavigation,
                    PaginationPageCount = pageCount
                })
                .ToList();
        }

        #endregion

        #region Page References

        public void UpdateSinglePageReference(string pageNavigation, int pageId)
        {
            var referencedPage = _db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == pageNavigation);
            var refs = _db.PageReferences
                .Where(r => r.ReferencesPageNavigation == pageNavigation && r.PageId == pageId);

            foreach (var r in refs)
            {
                r.ReferencesPageId = referencedPage?.Id;
            }

            _db.SaveChanges();
            FlushPageCache(pageId);
        }

        public void UpdatePageReferences(int pageId, List<PageReference> referencesPageNavigations)
        {
            _db.PageReferences.Where(r => r.PageId == pageId).ExecuteDelete();

            foreach (var refNav in referencesPageNavigations.DistinctBy(r => r.Navigation))
            {
                var referencedPage = _db.Pages.AsNoTracking()
                    .SingleOrDefault(p => p.Navigation == refNav.Navigation);

                var entity = new PageReferenceEntityDB
                {
                    PageId = pageId,
                    ReferencesPageNavigation = refNav.Navigation,
                    ReferencesPageId = referencedPage?.Id
                };

                _db.PageReferences.Add(entity);
            }

            _db.SaveChanges();
            FlushPageCache(pageId);
        }

        #endregion

        #region Page Listings

        public List<Page> GetAllPagesByInstructionPaged(int pageNumber, string? instruction = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            var query = from p in _db.Pages.AsNoTracking()
                        join profile in _db.Profiles.AsNoTracking() on p.ModifiedByUserId equals profile.UserId into profiles
                        from profile in profiles.DefaultIfEmpty()
                        where instruction == null || _db.ProcessingInstructions.Any(pi => pi.PageId == p.Id && pi.Instruction == instruction)
                        select new { p, profile };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

            return query
                .OrderBy(x => x.p.Name)
                .Skip(skip)
                .Take(pageSize)
                .Select(x => new Page
                {
                    Id = x.p.Id,
                    Name = x.p.Name,
                    Navigation = x.p.Navigation,
                    Description = x.p.Description,
                    ModifiedDate = x.p.ModifiedDate,
                    ModifiedByUserName = x.profile != null ? x.profile.AccountName : string.Empty,
                    PaginationPageCount = pageCount
                })
                .ToList();
        }

        public List<int> GetDeletedPageIdsByTokens(List<string>? tokens)
        {
            if (tokens == null || tokens.Count == 0) return [];

            var lowerTokens = tokens.Select(t => t.ToLowerInvariant()).ToList();

            return _db.DeletedPages.AsNoTracking()
                .Where(dp => lowerTokens.Any(t =>
                    EF.Functions.ILike(dp.Name, $"%{t}%") ||
                    EF.Functions.ILike(dp.Navigation, $"%{t}%")))
                .Select(dp => dp.Id)
                .ToList();
        }

        public List<int> GetPageIdsByTokens(List<string>? tokens)
        {
            if (tokens == null || tokens.Count == 0) return [];

            var lowerTokens = tokens.Select(t => t.ToLowerInvariant()).ToList();

            return _db.PageTokens.AsNoTracking()
                .Where(pt => lowerTokens.Contains(pt.Token.ToLower()))
                .Select(pt => pt.PageId)
                .Distinct()
                .ToList();
        }

        public List<Page> GetAllNamespacePagesPaged(int pageNumber, string namespaceName,
            string? orderBy = null, string? orderByDirection = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            var query = from p in _db.Pages.AsNoTracking()
                        join profile in _db.Profiles.AsNoTracking() on p.ModifiedByUserId equals profile.UserId into profiles
                        from profile in profiles.DefaultIfEmpty()
                        where p.Namespace == namespaceName
                        select new { p, profile };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

            var isAsc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);
            var orderedQuery = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "name" => isAsc ? query.OrderBy(x => x.p.Name) : query.OrderByDescending(x => x.p.Name),
                "modifieddate" => isAsc ? query.OrderBy(x => x.p.ModifiedDate) : query.OrderByDescending(x => x.p.ModifiedDate),
                _ => query.OrderBy(x => x.p.Name)
            };

            return orderedQuery
                .Skip(skip)
                .Take(pageSize)
                .Select(x => new Page
                {
                    Id = x.p.Id,
                    Name = x.p.Name,
                    Navigation = x.p.Navigation,
                    Description = x.p.Description,
                    ModifiedDate = x.p.ModifiedDate,
                    ModifiedByUserName = x.profile != null ? x.profile.AccountName : string.Empty,
                    PaginationPageCount = pageCount
                })
                .ToList();
        }

        public List<Page> GetAllPagesPaged(int pageNumber,
            string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            IQueryable<PageEntityDB> baseQuery = _db.Pages.AsNoTracking();

            if (searchTerms?.Count > 0)
            {
                var pageIds = GetPageIdsByTokens(searchTerms);
                baseQuery = baseQuery.Where(p => pageIds.Contains(p.Id));
            }

            var query = from p in baseQuery
                        join profile in _db.Profiles.AsNoTracking() on p.ModifiedByUserId equals profile.UserId into profiles
                        from profile in profiles.DefaultIfEmpty()
                        select new { p, profile, DeletedRevisionCount = _db.DeletedPageRevisions.Count(dpr => dpr.PageId == p.Id) };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

            var isAsc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);
            var orderedQuery = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "name" => isAsc ? query.OrderBy(x => x.p.Name) : query.OrderByDescending(x => x.p.Name),
                "modifieddate" => isAsc ? query.OrderBy(x => x.p.ModifiedDate) : query.OrderByDescending(x => x.p.ModifiedDate),
                "namespace" => isAsc ? query.OrderBy(x => x.p.Namespace) : query.OrderByDescending(x => x.p.Namespace),
                _ => query.OrderBy(x => x.p.Name)
            };

            return orderedQuery
                .Skip(skip)
                .Take(pageSize)
                .Select(x => new Page
                {
                    Id = x.p.Id,
                    Name = x.p.Name,
                    Navigation = x.p.Navigation,
                    Description = x.p.Description,
                    Revision = x.p.Revision,
                    ModifiedDate = x.p.ModifiedDate,
                    ModifiedByUserName = x.profile != null ? x.profile.AccountName : string.Empty,
                    DeletedRevisionCount = x.DeletedRevisionCount,
                    PaginationPageCount = pageCount
                })
                .ToList();
        }

        public List<Page> GetAllDeletedPagesPaged(int pageNumber, string? orderBy = null,
            string? orderByDirection = null, List<string>? searchTerms = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            IQueryable<DeletedPageEntityDB> baseQuery = _db.DeletedPages.AsNoTracking();

            if (searchTerms?.Count > 0)
            {
                var pageIds = GetDeletedPageIdsByTokens(searchTerms);
                baseQuery = baseQuery.Where(dp => pageIds.Contains(dp.Id));
            }

            var query = from dp in baseQuery
                        join profile in _db.Profiles.AsNoTracking() on dp.DeletedByUserId equals profile.UserId into profiles
                        from profile in profiles.DefaultIfEmpty()
                        select new { dp, profile };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

            var isAsc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);
            var orderedQuery = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "name" => isAsc ? query.OrderBy(x => x.dp.Name) : query.OrderByDescending(x => x.dp.Name),
                "deleteddate" => isAsc ? query.OrderBy(x => x.dp.DeletedDate) : query.OrderByDescending(x => x.dp.DeletedDate),
                _ => query.OrderByDescending(x => x.dp.DeletedDate)
            };

            return orderedQuery
                .Skip(skip)
                .Take(pageSize)
                .Select(x => new Page
                {
                    Id = x.dp.Id,
                    Name = x.dp.Name,
                    Navigation = x.dp.Navigation,
                    Description = x.dp.Description,
                    Revision = x.dp.Revision,
                    DeletedDate = x.dp.DeletedDate,
                    DeletedByUserName = x.profile != null ? x.profile.AccountName : string.Empty,
                    PaginationPageCount = pageCount
                })
                .ToList();
        }

        public List<NamespaceStat> GetAllNamespacesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            var query = _db.Pages.AsNoTracking()
                .GroupBy(p => p.Namespace)
                .Select(g => new NamespaceStat
                {
                    Namespace = g.Key,
                    CountOfPages = g.Count()
                });

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

            var isAsc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);
            query = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "namespace" => isAsc ? query.OrderBy(x => x.Namespace) : query.OrderByDescending(x => x.Namespace),
                "countofpages" => isAsc ? query.OrderBy(x => x.CountOfPages) : query.OrderByDescending(x => x.CountOfPages),
                _ => query.OrderBy(x => x.Namespace)
            };

            return query
                .Skip(skip)
                .Take(pageSize)
                .ToList()
                .Select(x => { x.PaginationPageCount = pageCount; return x; })
                .ToList();
        }

        public List<string> GetAllNamespaces()
        {
            return _db.Pages.AsNoTracking()
                .Select(p => p.Namespace)
                .Distinct()
                .OrderBy(n => n)
                .ToList();
        }

        public List<Page> GetAllPages()
        {
            return _db.Pages.AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => new Page
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    Revision = p.Revision
                })
                .ToList();
        }

        public List<Page> GetAllTemplatePages()
        {
            return _db.Pages.AsNoTracking()
                .Where(p => p.Namespace == "Template")
                .OrderBy(p => p.Name)
                .Select(p => new Page
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description
                })
                .ToList();
        }

        public List<FeatureTemplate> GetAllFeatureTemplates()
        {
            return WikiCache.AddOrGet(WikiCacheKeyFunction.Build(WikiCache.Category.Configuration), () =>
                (from p in _db.Pages.AsNoTracking()
                 join pi in _db.ProcessingInstructions.AsNoTracking() on p.Id equals pi.PageId
                 where pi.Instruction.StartsWith("template:")
                 select new FeatureTemplate
                 {
                     PageId = p.Id,
                     Name = p.Name,
                     Description = p.Description,
                     Type = pi.Instruction.Substring(9)
                 })
                .ToList()
            ).EnsureNotNull();
        }

        #endregion

        #region Processing Instructions

        public void UpdatePageProcessingInstructions(int pageId, List<string> instructions)
        {
            _db.ProcessingInstructions.Where(pi => pi.PageId == pageId).ExecuteDelete();

            var normalized = instructions.Select(i => i.ToLowerInvariant()).Distinct();
            foreach (var instruction in normalized)
            {
                _db.ProcessingInstructions.Add(new ProcessingInstructionEntityDB
                {
                    PageId = pageId,
                    Instruction = instruction
                });
            }

            _db.SaveChanges();
            FlushPageCache(pageId);
        }

        #endregion

        #region Page CRUD

        public Page? GetPageRevisionById(int pageId, int? revision = null)
        {
            return WikiCache.AddOrGet(WikiCacheKeyFunction.Build(WikiCache.Category.Page, [pageId, revision]), () =>
            {
                var page = _db.Pages.AsNoTracking().SingleOrDefault(p => p.Id == pageId);
                if (page == null) return null;

                var effectiveRevision = revision ?? page.Revision;
                var pr = _db.PageRevisions.AsNoTracking()
                    .SingleOrDefault(r => r.PageId == pageId && r.Revision == effectiveRevision);

                if (pr == null) return null;

                return MapToPage(page, pr, effectiveRevision);
            });
        }

        public void SavePageSearchTokens(List<PageToken> items)
        {
            var distinctItems = items.DistinctBy(i => new { i.PageId, i.Token }).ToList();

            foreach (var item in distinctItems)
            {
                var existing = _db.PageTokens.SingleOrDefault(pt => pt.PageId == item.PageId && pt.Token == item.Token);
                if (existing != null)
                {
                    existing.DoubleMetaphone = item.DoubleMetaphone;
                    existing.Weight = item.Weight;
                }
                else
                {
                    _db.PageTokens.Add(new PageTokenEntityDB
                    {
                        PageId = item.PageId,
                        Token = item.Token,
                        DoubleMetaphone = item.DoubleMetaphone,
                        Weight = item.Weight
                    });
                }
            }

            _db.SaveChanges();
        }

        public void TruncateAllPageRevisions(string confirm)
        {
            if (confirm != "YES") return;

            using var transaction = _db.Database.BeginTransaction();
            try
            {
                _db.PageRevisions.ExecuteDelete();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public int GetCurrentPageRevision(int pageId)
        {
            return WikiCache.AddOrGet(WikiCacheKeyFunction.Build(WikiCache.Category.Page, [pageId]), () =>
                _db.Pages.AsNoTracking()
                    .Where(p => p.Id == pageId)
                    .Select(p => p.Revision)
                    .SingleOrDefault()
            );
        }

        public Page? GetLimitedPageInfoByIdAndRevision(int pageId, int? revision = null)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [pageId, revision]);
            return WikiCache.AddOrGet(cacheKey, () =>
            {
                var page = _db.Pages.AsNoTracking().SingleOrDefault(p => p.Id == pageId);
                if (page == null) return null;

                var effectiveRevision = revision ?? page.Revision;
                var pr = _db.PageRevisions.AsNoTracking()
                    .SingleOrDefault(r => r.PageId == pageId && r.Revision == effectiveRevision);

                if (pr == null) return null;

                return new Page
                {
                    Id = page.Id,
                    Name = pr.Name,
                    Navigation = page.Navigation,
                    Description = pr.Description,
                    Revision = effectiveRevision,
                    DataHash = pr.DataHash,
                    ChangeSummary = pr.ChangeSummary
                };
            });
        }

        public int SavePage(Page page)
        {
            var navigation = NamespaceNavigation.CleanAndValidate(page.Name);
            var newDataHash = TightWiki.Utils.Security.Helpers.Crc32(page.Body ?? string.Empty);
            var pageNamespace = page.Name.Contains("::") ? page.Name.Substring(0, page.Name.IndexOf("::")).Trim() : string.Empty;

            using var transaction = _db.Database.BeginTransaction();
            try
            {
                int currentPageRevision = 0;
                bool hasPageChanged = false;

                if (page.Id == 0)
                {
                    var newPage = new PageEntityDB
                    {
                        Name = page.Name,
                        Navigation = navigation,
                        Namespace = pageNamespace,
                        Description = page.Description,
                        Revision = 0,
                        CreatedByUserId = page.CreatedByUserId,
                        CreatedDate = page.CreatedDate,
                        ModifiedByUserId = page.ModifiedByUserId,
                        ModifiedDate = DateTime.UtcNow
                    };

                    _db.Pages.Add(newPage);
                    _db.SaveChanges();
                    page.Id = newPage.Id;
                    hasPageChanged = true;
                }
                else
                {
                    var existingPage = _db.Pages.Single(p => p.Id == page.Id);
                    var currentRevisionInfo = GetLimitedPageInfoByIdAndRevisionInternal(page.Id)
                        ?? throw new System.Exception("The page could not be found.");

                    currentPageRevision = currentRevisionInfo.Revision;

                    existingPage.Name = page.Name;
                    existingPage.Navigation = navigation;
                    existingPage.Namespace = pageNamespace;
                    existingPage.Description = page.Description;
                    existingPage.ModifiedByUserId = page.ModifiedByUserId;
                    existingPage.ModifiedDate = DateTime.UtcNow;
                    _db.SaveChanges();

                    hasPageChanged = currentRevisionInfo.Name != page.Name
                        || currentRevisionInfo.Description != page.Description
                        || currentRevisionInfo.ChangeSummary != page.ChangeSummary
                        || currentRevisionInfo.DataHash != newDataHash;
                }

                if (hasPageChanged)
                {
                    currentPageRevision++;

                    var pageEntity = _db.Pages.Single(p => p.Id == page.Id);
                    pageEntity.Revision = currentPageRevision;
                    _db.SaveChanges();

                    var newRevision = new PageRevisionEntityDB
                    {
                        PageId = page.Id,
                        Revision = currentPageRevision,
                        Name = page.Name,
                        Navigation = navigation,
                        Namespace = pageNamespace,
                        Description = page.Description,
                        Body = page.Body ?? string.Empty,
                        DataHash = newDataHash,
                        ChangeSummary = page.ChangeSummary ?? string.Empty,
                        ModifiedByUserId = page.ModifiedByUserId,
                        ModifiedDate = DateTime.UtcNow
                    };

                    _db.PageRevisions.Add(newRevision);
                    _db.SaveChanges();

                    // Reassociate attachments with new page revision
                    var currentAttachments = _db.PageRevisionAttachments
                        .Where(a => a.PageId == page.Id && a.PageRevision == currentPageRevision - 1)
                        .ToList();

                    foreach (var attachment in currentAttachments)
                    {
                        _db.PageRevisionAttachments.Add(new DAL.Models.PageRevisionAttachmentEntityDB
                        {
                            PageId = page.Id,
                            PageFileId = attachment.PageFileId,
                            FileRevision = attachment.FileRevision,
                            PageRevision = currentPageRevision
                        });
                    }

                    _db.SaveChanges();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            return page.Id;
        }

        // Internal version without caching for use within SavePage transaction
        private Page? GetLimitedPageInfoByIdAndRevisionInternal(int pageId, int? revision = null)
        {
            var page = _db.Pages.AsNoTracking().SingleOrDefault(p => p.Id == pageId);
            if (page == null) return null;

            var effectiveRevision = revision ?? page.Revision;
            var pr = _db.PageRevisions.AsNoTracking()
                .SingleOrDefault(r => r.PageId == pageId && r.Revision == effectiveRevision);

            if (pr == null) return null;

            return new Page
            {
                Id = page.Id,
                Name = pr.Name,
                Navigation = page.Navigation,
                Description = pr.Description,
                Revision = effectiveRevision,
                DataHash = pr.DataHash,
                ChangeSummary = pr.ChangeSummary
            };
        }

        public Page? GetPageInfoByNavigation(string navigation)
        {
            return WikiCache.AddOrGet(WikiCacheKeyFunction.Build(WikiCache.Category.Page, [navigation]), () =>
            {
                var page = _db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == navigation);
                if (page == null) return null;

                return new Page
                {
                    Id = page.Id,
                    Name = page.Name,
                    Navigation = page.Navigation,
                    Description = page.Description,
                    Revision = page.Revision,
                    CreatedDate = page.CreatedDate,
                    ModifiedDate = page.ModifiedDate
                };
            });
        }

        public int GetPageRevisionCountByPageId(int pageId)
        {
            return WikiCache.AddOrGet(WikiCacheKeyFunction.Build(WikiCache.Category.Page, [pageId]), () =>
                _db.PageRevisions.AsNoTracking().Count(r => r.PageId == pageId)
            );
        }

        #endregion

        #region Deleted Pages

        public void RestoreDeletedPageByPageId(int pageId)
        {
            using var transaction = _db.Database.BeginTransaction();
            try
            {
                var deletedPage = _db.DeletedPages.SingleOrDefault(dp => dp.Id == pageId);
                if (deletedPage == null) return;

                var restoredPage = new PageEntityDB
                {
                    Id = deletedPage.Id,
                    Name = deletedPage.Name,
                    Navigation = deletedPage.Navigation,
                    Namespace = deletedPage.Namespace,
                    Description = deletedPage.Description,
                    Revision = deletedPage.Revision,
                    CreatedByUserId = deletedPage.CreatedByUserId,
                    CreatedDate = deletedPage.CreatedDate,
                    ModifiedByUserId = deletedPage.ModifiedByUserId,
                    ModifiedDate = deletedPage.ModifiedDate
                };

                _db.Pages.Add(restoredPage);
                _db.DeletedPages.Remove(deletedPage);
                _db.SaveChanges();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            FlushPageCache(pageId);
        }

        public void MovePageRevisionToDeletedById(int pageId, int revision, Guid userId)
        {
            using var transaction = _db.Database.BeginTransaction();
            try
            {
                var pageRevision = _db.PageRevisions.SingleOrDefault(pr => pr.PageId == pageId && pr.Revision == revision);
                if (pageRevision == null) return;

                var deletedRevision = new DeletedPageRevisionEntityDB
                {
                    PageId = pageRevision.PageId,
                    Revision = pageRevision.Revision,
                    Name = pageRevision.Name,
                    Navigation = pageRevision.Navigation,
                    Namespace = pageRevision.Namespace,
                    Description = pageRevision.Description,
                    Body = pageRevision.Body,
                    DataHash = pageRevision.DataHash,
                    ChangeSummary = pageRevision.ChangeSummary,
                    ModifiedByUserId = pageRevision.ModifiedByUserId,
                    ModifiedDate = pageRevision.ModifiedDate,
                    DeletedByUserId = userId,
                    DeletedDate = DateTime.UtcNow
                };

                _db.DeletedPageRevisions.Add(deletedRevision);
                _db.PageRevisions.Remove(pageRevision);
                _db.SaveChanges();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            FlushPageCache(pageId);
        }

        public void MovePageToDeletedById(int pageId, Guid userId)
        {
            using var transaction = _db.Database.BeginTransaction();
            try
            {
                var page = _db.Pages.SingleOrDefault(p => p.Id == pageId);
                if (page == null) return;

                var deletedPage = new DeletedPageEntityDB
                {
                    Id = page.Id,
                    Name = page.Name,
                    Navigation = page.Navigation,
                    Namespace = page.Namespace,
                    Description = page.Description,
                    Revision = page.Revision,
                    CreatedByUserId = page.CreatedByUserId,
                    CreatedDate = page.CreatedDate,
                    ModifiedByUserId = page.ModifiedByUserId,
                    ModifiedDate = page.ModifiedDate,
                    DeletedByUserId = userId,
                    DeletedDate = DateTime.UtcNow
                };

                _db.DeletedPages.Add(deletedPage);
                _db.Pages.Remove(page);

                // Also delete related data
                _db.PageTags.Where(t => t.PageId == pageId).ExecuteDelete();
                _db.ProcessingInstructions.Where(pi => pi.PageId == pageId).ExecuteDelete();
                _db.PageComments.Where(c => c.PageId == pageId).ExecuteDelete();
                _db.PageReferences.Where(r => r.PageId == pageId).ExecuteDelete();
                _db.PageTokens.Where(t => t.PageId == pageId).ExecuteDelete();
                _db.CompilationStatistics.Where(cs => cs.PageId == pageId).ExecuteDelete();

                _db.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            FlushPageCache(pageId);
        }

        public void PurgeDeletedPageByPageId(int pageId)
        {
            _db.DeletedPages.Where(dp => dp.Id == pageId).ExecuteDelete();
            PurgeDeletedPageRevisionsByPageId(pageId);
            FlushPageCache(pageId);
        }

        public void PurgeDeletedPages()
        {
            _db.DeletedPages.ExecuteDelete();
            PurgeDeletedPageRevisions();
        }

        public int GetCountOfPageAttachmentsById(int pageId)
        {
            return _db.PageFiles.AsNoTracking().Count(pf => pf.PageId == pageId);
        }

        public Page? GetDeletedPageById(int pageId)
        {
            var dp = (from deletedPage in _db.DeletedPages.AsNoTracking()
                      join profile in _db.Profiles.AsNoTracking() on deletedPage.DeletedByUserId equals profile.UserId into profiles
                      from profile in profiles.DefaultIfEmpty()
                      where deletedPage.Id == pageId
                      select new { deletedPage, profile }).SingleOrDefault();

            if (dp == null) return null;

            return new Page
            {
                Id = dp.deletedPage.Id,
                Name = dp.deletedPage.Name,
                Navigation = dp.deletedPage.Navigation,
                Description = dp.deletedPage.Description,
                Revision = dp.deletedPage.Revision,
                DeletedDate = dp.deletedPage.DeletedDate,
                DeletedByUserName = dp.profile?.AccountName ?? string.Empty
            };
        }

        public Page? GetLatestPageRevisionById(int pageId)
        {
            var result = (from p in _db.Pages.AsNoTracking()
                          join pr in _db.PageRevisions.AsNoTracking() on new { p.Id, p.Revision } equals new { Id = pr.PageId, pr.Revision }
                          join profile in _db.Profiles.AsNoTracking() on pr.ModifiedByUserId equals profile.UserId into profiles
                          from profile in profiles.DefaultIfEmpty()
                          where p.Id == pageId
                          select new { p, pr, profile }).SingleOrDefault();

            if (result == null) return null;

            return MapToPage(result.p, result.pr, result.p.Revision, result.profile?.AccountName);
        }

        public int GetPageNextRevision(int pageId, int revision)
        {
            return _db.PageRevisions.AsNoTracking()
                .Where(pr => pr.PageId == pageId && pr.Revision > revision)
                .OrderBy(pr => pr.Revision)
                .Select(pr => pr.Revision)
                .FirstOrDefault();
        }

        public int GetPagePreviousRevision(int pageId, int revision)
        {
            return _db.PageRevisions.AsNoTracking()
                .Where(pr => pr.PageId == pageId && pr.Revision < revision)
                .OrderByDescending(pr => pr.Revision)
                .Select(pr => pr.Revision)
                .FirstOrDefault();
        }

        #endregion

        #region Deleted Page Revisions

        public List<DeletedPageRevision> GetDeletedPageRevisionsByIdPaged(int pageId, int pageNumber,
            string? orderBy = null, string? orderByDirection = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            var query = from dpr in _db.DeletedPageRevisions.AsNoTracking()
                        join profile in _db.Profiles.AsNoTracking() on dpr.DeletedByUserId equals profile.UserId into profiles
                        from profile in profiles.DefaultIfEmpty()
                        where dpr.PageId == pageId
                        select new { dpr, profile };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

            var isAsc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);
            var orderedQuery = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "revision" => isAsc ? query.OrderBy(x => x.dpr.Revision) : query.OrderByDescending(x => x.dpr.Revision),
                "deleteddate" => isAsc ? query.OrderBy(x => x.dpr.DeletedDate) : query.OrderByDescending(x => x.dpr.DeletedDate),
                _ => query.OrderByDescending(x => x.dpr.Revision)
            };

            return orderedQuery
                .Skip(skip)
                .Take(pageSize)
                .Select(x => new DeletedPageRevision
                {
                    Id = x.dpr.PageId,
                    Revision = x.dpr.Revision,
                    Name = x.dpr.Name,
                    Navigation = x.dpr.Navigation,
                    Description = x.dpr.Description,
                    Body = x.dpr.Body,
                    DeletedDate = x.dpr.DeletedDate,
                    DeletedByUserName = x.profile != null ? x.profile.AccountName : string.Empty,
                    PaginationPageSize = pageSize
                })
                .ToList();
        }

        public void PurgeDeletedPageRevisions()
        {
            _db.DeletedPageRevisions.ExecuteDelete();
        }

        public void PurgeDeletedPageRevisionsByPageId(int pageId)
        {
            _db.DeletedPageRevisions.Where(dpr => dpr.PageId == pageId).ExecuteDelete();
            FlushPageCache(pageId);
        }

        public void PurgeDeletedPageRevisionByPageIdAndRevision(int pageId, int revision)
        {
            _db.DeletedPageRevisions
                .Where(dpr => dpr.PageId == pageId && dpr.Revision == revision)
                .ExecuteDelete();
            FlushPageCache(pageId);
        }

        public void RestoreDeletedPageRevisionByPageIdAndRevision(int pageId, int revision)
        {
            var deletedRevision = _db.DeletedPageRevisions
                .SingleOrDefault(dpr => dpr.PageId == pageId && dpr.Revision == revision);

            if (deletedRevision == null) return;

            var restoredRevision = new PageRevisionEntityDB
            {
                PageId = deletedRevision.PageId,
                Revision = deletedRevision.Revision,
                Name = deletedRevision.Name,
                Navigation = deletedRevision.Navigation,
                Namespace = deletedRevision.Namespace,
                Description = deletedRevision.Description,
                Body = deletedRevision.Body,
                DataHash = deletedRevision.DataHash,
                ChangeSummary = deletedRevision.ChangeSummary,
                ModifiedByUserId = deletedRevision.ModifiedByUserId,
                ModifiedDate = deletedRevision.ModifiedDate
            };

            _db.PageRevisions.Add(restoredRevision);
            _db.DeletedPageRevisions.Remove(deletedRevision);
            _db.SaveChanges();
            FlushPageCache(pageId);
        }

        public DeletedPageRevision? GetDeletedPageRevisionById(int pageId, int revision)
        {
            var result = (from dpr in _db.DeletedPageRevisions.AsNoTracking()
                          join profile in _db.Profiles.AsNoTracking() on dpr.DeletedByUserId equals profile.UserId into profiles
                          from profile in profiles.DefaultIfEmpty()
                          where dpr.PageId == pageId && dpr.Revision == revision
                          select new { dpr, profile }).SingleOrDefault();

            if (result == null) return null;

            return new DeletedPageRevision
            {
                Id = result.dpr.PageId,
                Revision = result.dpr.Revision,
                Name = result.dpr.Name,
                Navigation = result.dpr.Navigation,
                Description = result.dpr.Description,
                Body = result.dpr.Body,
                DeletedDate = result.dpr.DeletedDate,
                DeletedByUserName = result.profile?.AccountName ?? string.Empty
            };
        }

        #endregion

        #region Page Revision by Navigation

        public Page? GetPageRevisionByNavigation(NamespaceNavigation navigation, int? revision = null)
        {
            return GetPageRevisionByNavigation(navigation.Canonical, revision, false);
        }

        public Page? GetPageRevisionByNavigation(string givenNavigation, int? revision = null, bool refreshCache = false)
        {
            var navigation = new NamespaceNavigation(givenNavigation);
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [navigation.Canonical, revision]);

            if (refreshCache)
            {
                WikiCache.Remove(cacheKey);
            }

            return WikiCache.AddOrGet(cacheKey, () =>
            {
                var page = _db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == navigation.Canonical);
                if (page == null) return null;

                var effectiveRevision = revision ?? page.Revision;

                var result = (from pr in _db.PageRevisions.AsNoTracking()
                              join profile in _db.Profiles.AsNoTracking() on pr.ModifiedByUserId equals profile.UserId into profiles
                              from profile in profiles.DefaultIfEmpty()
                              where pr.PageId == page.Id && pr.Revision == effectiveRevision
                              select new { pr, profile }).SingleOrDefault();

                if (result == null) return null;

                return MapToPage(page, result.pr, effectiveRevision, result.profile?.AccountName);
            });
        }

        #endregion

        #region Tags

        public List<TagAssociation> GetAssociatedTags(string tag)
        {
            return (from pt in _db.PageTags.AsNoTracking()
                    join p in _db.Pages.AsNoTracking() on pt.PageId equals p.Id
                    where _db.PageTags.Any(t => t.PageId == p.Id && t.Tag == tag) && pt.Tag != tag
                    group pt by pt.Tag into g
                    select new TagAssociation
                    {
                        Tag = g.Key,
                        PageCount = g.Count()
                    })
                .OrderByDescending(t => t.PageCount)
                .ToList();
        }

        public List<Page> GetPageInfoByNamespaces(List<string> namespaces)
        {
            return _db.Pages.AsNoTracking()
                .Where(p => namespaces.Contains(p.Namespace))
                .Select(p => new Page
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description
                })
                .ToList();
        }

        public List<Page> GetPageInfoByTags(List<string> tags)
        {
            var pageIds = _db.PageTags.AsNoTracking()
                .Where(t => tags.Contains(t.Tag))
                .Select(t => t.PageId)
                .Distinct()
                .ToList();

            return _db.Pages.AsNoTracking()
                .Where(p => pageIds.Contains(p.Id))
                .Select(p => new Page
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description
                })
                .ToList();
        }

        public List<Page> GetPageInfoByTag(string tag)
        {
            return GetPageInfoByTags([tag]);
        }

        public void UpdatePageTags(int pageId, List<string> tags)
        {
            _db.PageTags.Where(t => t.PageId == pageId).ExecuteDelete();

            var normalized = tags.Select(t => t.ToLowerInvariant()).Distinct();
            foreach (var tag in normalized)
            {
                _db.PageTags.Add(new PageTagEntityDB
                {
                    PageId = pageId,
                    Tag = tag
                });
            }

            _db.SaveChanges();
        }

        #endregion

        #region Mapping Helpers

        private static Page MapToPage(PageEntityDB page, PageRevisionEntityDB revision, int currentRevision, string? modifiedByUserName = null)
        {
            return new Page
            {
                Id = page.Id,
                Name = revision.Name,
                Navigation = page.Navigation,
                Description = revision.Description,
                Body = revision.Body,
                Revision = currentRevision,
                MostCurrentRevision = page.Revision,
                DataHash = revision.DataHash,
                ChangeSummary = revision.ChangeSummary,
                CreatedByUserId = page.CreatedByUserId,
                CreatedDate = page.CreatedDate,
                ModifiedByUserId = revision.ModifiedByUserId,
                ModifiedDate = revision.ModifiedDate,
                ModifiedByUserName = modifiedByUserName ?? string.Empty,
                HigherRevisionCount = page.Revision - currentRevision
            };
        }

        #endregion
    }
}

