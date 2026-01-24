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
    public sealed class PageRepositoryEf : IPageRepository
    {
        public WikiDbContext Db { get; }

        public PageRepositoryEf(WikiDbContext db)
        {
            Db = db;
        }

        public IEnumerable<ApiPage> AutoCompletePage(string? searchText)
        {
            var text = searchText ?? string.Empty;
            return Db.Pages.AsNoTracking()
                .Where(p => EF.Functions.ILike(p.Name, $"%{text}%"))
                .OrderBy(p => p.Name)
                .Take(25)
                .Select(p => new ApiPage
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
            return Db.Pages.AsNoTracking()
                .Where(p => EF.Functions.ILike(p.Namespace, $"%{text}%"))
                .Select(p => p.Namespace)
                .Distinct()
                .OrderBy(n => n)
                .Take(25)
                .ToList();
        }

        public ApiPage? GetPageRevisionInfoById(int pageId, int? revision = null)
        {
            var page = Db.Pages.AsNoTracking().SingleOrDefault(p => p.Id == pageId);
            if (page == null) return null;

            var effectiveRevision = revision ?? page.Revision;

            var pr = Db.PageRevisions.AsNoTracking()
                .SingleOrDefault(r => r.PageId == pageId && r.Revision == effectiveRevision);

            if (pr == null) return null;

            return MapToApiPage(page, pr, effectiveRevision);
        }

        public ApiProcessingInstructionCollection GetPageProcessingInstructionsByPageId(int pageId)
        {
            var instructions = Db.ProcessingInstructions.AsNoTracking()
                .Where(pi => pi.PageId == pageId)
                .Select(pi => new ApiProcessingInstruction { Instruction = pi.Instruction })
                .ToList();

            return new ApiProcessingInstructionCollection { Collection = instructions };
        }

        public List<ApiPageTag> GetPageTagsById(int pageId)
        {
            return Db.PageTags.AsNoTracking()
                .Where(t => t.PageId == pageId)
                .Select(t => new ApiPageTag
                {
                    Id = t.Id,
                    PageId = t.PageId,
                    Tag = t.Tag
                })
                .ToList();
        }

        public List<ApiPageRevision> GetPageRevisionsInfoByNavigationPaged(string navigation, int pageNumber,
            string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize.Value;

            var page = Db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == navigation);
            if (page == null) return [];

            var query = from pr in Db.PageRevisions.AsNoTracking()
                        join profile in Db.Profiles.AsNoTracking() on pr.ModifiedByUserId equals profile.UserId into profiles
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
                .Select(x => new ApiPageRevision
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

        public List<ApiPageRevision> GetTopRecentlyModifiedPagesInfoByUserId(Guid userId, int topCount)
        {
            return (from pr in Db.PageRevisions.AsNoTracking()
                    join p in Db.Pages.AsNoTracking() on pr.PageId equals p.Id
                    where pr.ModifiedByUserId == userId
                    orderby pr.ModifiedDate descending
                    select new ApiPageRevision
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
            return Db.Pages.AsNoTracking()
                .Where(p => p.Id == pageId)
                .Select(p => p.Navigation)
                .SingleOrDefault();
        }

        public List<ApiPage> GetTopRecentlyModifiedPagesInfo(int topCount)
        {
            return Db.Pages.AsNoTracking()
                .OrderByDescending(p => p.ModifiedDate)
                .Take(topCount)
                .Select(p => new ApiPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    ModifiedDate = p.ModifiedDate
                })
                .ToList();
        }

        public List<ApiPage> PageSearch(List<string> searchTerms)
        {
            if (searchTerms.Count == 0) return [];

            var allowFuzzyMatching = ConfigurationRepository.Get<bool>("Search", "Allow Fuzzy Matching");
            var meteredSearchTokens = GetMeteredPageSearchTokens(searchTerms, allowFuzzyMatching);
            if (meteredSearchTokens.Count == 0) return [];

            var pageIds = meteredSearchTokens.Select(t => t.PageId).ToList();
            var maxScore = meteredSearchTokens.Max(t => t.Score);

            return (from p in Db.Pages.AsNoTracking()
                    join profile in Db.Profiles.AsNoTracking() on p.ModifiedByUserId equals profile.UserId into profiles
                    from profile in profiles.DefaultIfEmpty()
                    where pageIds.Contains(p.Id)
                    select new ApiPage
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

        public List<ApiPage> PageSearchPaged(List<string> searchTerms, int pageNumber, int? pageSize = null, bool? allowFuzzyMatching = null)
        {
            if (searchTerms.Count == 0) return [];

            pageSize ??= GlobalConfiguration.PaginationSize;
            allowFuzzyMatching ??= ConfigurationRepository.Get<bool>("Search", "Allow Fuzzy Matching");

            var meteredSearchTokens = GetMeteredPageSearchTokens(searchTerms, allowFuzzyMatching.Value);
            if (meteredSearchTokens.Count == 0) return [];

            var skip = (pageNumber - 1) * pageSize.Value;
            var pageIds = meteredSearchTokens.Select(t => t.PageId).ToList();
            var tokenLookup = meteredSearchTokens.ToDictionary(t => t.PageId);

            var query = from p in Db.Pages.AsNoTracking()
                        join profile in Db.Profiles.AsNoTracking() on p.ModifiedByUserId equals profile.UserId into profiles
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
                return new ApiPage
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

        private List<ApiPageSearchToken> GetMeteredPageSearchTokens(List<string> searchTerms, bool allowFuzzyMatching)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Search, [string.Join(',', searchTerms), allowFuzzyMatching]);

            return WikiCache.AddOrGet(cacheKey, () =>
            {
                var minimumMatchScore = ConfigurationRepository.Get<float>("Search", "Minimum Match Score");

                var searchTokens = searchTerms.Select(t => new ApiPageToken
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
                        .Select(g => new ApiPageSearchToken
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

        private List<ApiPageSearchToken> GetExactPageSearchTokens(List<ApiPageToken> tokens, double minimumMatchScore)
        {
            var tokenStrings = tokens.Select(t => t.Token.ToLowerInvariant()).ToList();

            return Db.PageTokens.AsNoTracking()
                .Where(pt => tokenStrings.Contains(pt.Token.ToLower()))
                .GroupBy(pt => pt.PageId)
                .Select(g => new ApiPageSearchToken
                {
                    PageId = g.Key,
                    Match = g.Count() / (double)tokens.Count,
                    Weight = g.Sum(x => x.Weight),
                    Score = (g.Count() / (double)tokens.Count) * g.Sum(x => x.Weight)
                })
                .Where(t => t.Score >= minimumMatchScore)
                .ToList();
        }

        private List<ApiPageSearchToken> GetFuzzyPageSearchTokens(List<ApiPageToken> tokens, double minimumMatchScore)
        {
            var metaphones = tokens.Select(t => t.DoubleMetaphone).Where(m => !string.IsNullOrEmpty(m)).ToList();

            return Db.PageTokens.AsNoTracking()
                .Where(pt => metaphones.Contains(pt.DoubleMetaphone))
                .GroupBy(pt => pt.PageId)
                .Select(g => new ApiPageSearchToken
                {
                    PageId = g.Key,
                    Match = g.Count() / (double)tokens.Count * 0.8,
                    Weight = g.Sum(x => x.Weight) * 0.8,
                    Score = (g.Count() / (double)tokens.Count) * g.Sum(x => x.Weight) * 0.8
                })
                .Where(t => t.Score >= minimumMatchScore)
                .ToList();
        }

        public List<ApiRelatedPage> GetSimilarPagesPaged(int pageId, int similarity, int pageNumber, int? pageSize = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize.Value;

            var pageTags = Db.PageTags.AsNoTracking()
                .Where(t => t.PageId == pageId)
                .Select(t => t.Tag)
                .ToList();

            if (pageTags.Count == 0) return [];

            var query = from pt in Db.PageTags.AsNoTracking()
                        join p in Db.Pages.AsNoTracking() on pt.PageId equals p.Id
                        where pt.PageId != pageId && pageTags.Contains(pt.Tag)
                        group p by new { p.Id, p.Name, p.Navigation } into g
                        where g.Count() >= similarity
                        select new ApiRelatedPage
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

        public List<ApiRelatedPage> GetRelatedPagesPaged(int pageId, int pageNumber, int? pageSize = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize.Value;

            var query = from pr in Db.PageReferences.AsNoTracking()
                        join p in Db.Pages.AsNoTracking() on pr.ReferencesPageId equals p.Id
                        where pr.PageId == pageId && pr.ReferencesPageId != null
                        select new ApiRelatedPage
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

        public void InsertPageComment(int pageId, Guid userId, string body)
        {
            var entity = new DalPageCommentEntity
            {
                PageId = pageId,
                UserId = userId,
                Body = body,
                CreatedDate = DateTime.UtcNow
            };

            Db.PageComments.Add(entity);
            Db.SaveChanges();
        }

        public void DeletePageCommentById(int pageId, int commentId)
        {
            var entity = Db.PageComments.SingleOrDefault(c => c.Id == commentId && c.PageId == pageId);
            if (entity != null)
            {
                Db.PageComments.Remove(entity);
                Db.SaveChanges();
            }
        }

        public void DeletePageCommentByUserAndId(int pageId, Guid userId, int commentId)
        {
            var entity = Db.PageComments.SingleOrDefault(c => c.Id == commentId && c.PageId == pageId && c.UserId == userId);
            if (entity != null)
            {
                Db.PageComments.Remove(entity);
                Db.SaveChanges();
            }
        }

        public List<ApiPageComment> GetPageCommentsPaged(string navigation, int pageNumber)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            var page = Db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == navigation);
            if (page == null) return [];

            var query = from c in Db.PageComments.AsNoTracking()
                        join profile in Db.Profiles.AsNoTracking() on c.UserId equals profile.UserId into profiles
                        from profile in profiles.DefaultIfEmpty()
                        where c.PageId == page.Id
                        select new { c, profile };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

            return query
                .OrderByDescending(x => x.c.CreatedDate)
                .Skip(skip)
                .Take(pageSize)
                .Select(x => new ApiPageComment
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
        }

        public List<ApiNonexistentPage> GetMissingPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            var query = from pr in Db.PageReferences.AsNoTracking()
                        join p in Db.Pages.AsNoTracking() on pr.PageId equals p.Id
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
                .Select(x => new ApiNonexistentPage
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

        public void UpdateSinglePageReference(string pageNavigation, int pageId)
        {
            var referencedPage = Db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == pageNavigation);
            var refs = Db.PageReferences
                .Where(r => r.ReferencesPageNavigation == pageNavigation && r.PageId == pageId);

            foreach (var r in refs)
            {
                r.ReferencesPageId = referencedPage?.Id;
            }

            Db.SaveChanges();
        }

        public void UpdatePageReferences(int pageId, List<ApiPageReference> referencesPageNavigations)
        {
            Db.PageReferences.Where(r => r.PageId == pageId).ExecuteDelete();

            foreach (var refNav in referencesPageNavigations.DistinctBy(r => r.Navigation))
            {
                var referencedPage = Db.Pages.AsNoTracking()
                    .SingleOrDefault(p => p.Navigation == refNav.Navigation);

                var entity = new DalPageReferenceEntity
                {
                    PageId = pageId,
                    ReferencesPageNavigation = refNav.Navigation,
                    ReferencesPageId = referencedPage?.Id
                };

                Db.PageReferences.Add(entity);
            }

            Db.SaveChanges();
        }

        public List<ApiPage> GetAllPagesByInstructionPaged(int pageNumber, string? instruction = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            var query = from p in Db.Pages.AsNoTracking()
                        join profile in Db.Profiles.AsNoTracking() on p.ModifiedByUserId equals profile.UserId into profiles
                        from profile in profiles.DefaultIfEmpty()
                        where instruction == null || Db.ProcessingInstructions.Any(pi => pi.PageId == p.Id && pi.Instruction == instruction)
                        select new { p, profile };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

            return query
                .OrderBy(x => x.p.Name)
                .Skip(skip)
                .Take(pageSize)
                .Select(x => new ApiPage
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

            return Db.DeletedPages.AsNoTracking()
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

            return Db.PageTokens.AsNoTracking()
                .Where(pt => lowerTokens.Contains(pt.Token.ToLower()))
                .Select(pt => pt.PageId)
                .Distinct()
                .ToList();
        }

        public List<ApiPage> GetAllNamespacePagesPaged(int pageNumber, string namespaceName,
            string? orderBy = null, string? orderByDirection = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            var query = from p in Db.Pages.AsNoTracking()
                        join profile in Db.Profiles.AsNoTracking() on p.ModifiedByUserId equals profile.UserId into profiles
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
                .Select(x => new ApiPage
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

        public List<ApiPage> GetAllPagesPaged(int pageNumber, string? orderBy = null,
            string? orderByDirection = null, List<string>? searchTerms = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            IQueryable<DalPageEntity> baseQuery = Db.Pages.AsNoTracking();

            if (searchTerms?.Count > 0)
            {
                var pageIds = GetPageIdsByTokens(searchTerms);
                baseQuery = baseQuery.Where(p => pageIds.Contains(p.Id));
            }

            var query = from p in baseQuery
                        join profile in Db.Profiles.AsNoTracking() on p.ModifiedByUserId equals profile.UserId into profiles
                        from profile in profiles.DefaultIfEmpty()
                        select new { p, profile, DeletedRevisionCount = Db.DeletedPageRevisions.Count(dpr => dpr.PageId == p.Id) };

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
                .Select(x => new ApiPage
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

        public List<ApiPage> GetAllDeletedPagesPaged(int pageNumber, string? orderBy = null,
            string? orderByDirection = null, List<string>? searchTerms = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            IQueryable<DalDeletedPageEntity> baseQuery = Db.DeletedPages.AsNoTracking();

            if (searchTerms?.Count > 0)
            {
                var pageIds = GetDeletedPageIdsByTokens(searchTerms);
                baseQuery = baseQuery.Where(dp => pageIds.Contains(dp.Id));
            }

            var query = from dp in baseQuery
                        join profile in Db.Profiles.AsNoTracking() on dp.DeletedByUserId equals profile.UserId into profiles
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
                .Select(x => new ApiPage
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

        public List<ApiNamespaceStat> GetAllNamespacesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            var query = Db.Pages.AsNoTracking()
                .GroupBy(p => p.Namespace)
                .Select(g => new ApiNamespaceStat
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
            return Db.Pages.AsNoTracking()
                .Select(p => p.Namespace)
                .Distinct()
                .OrderBy(n => n)
                .ToList();
        }

        public List<ApiPage> GetAllPages()
        {
            return Db.Pages.AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => new ApiPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description,
                    Revision = p.Revision
                })
                .ToList();
        }

        public List<ApiPage> GetAllTemplatePages()
        {
            return Db.Pages.AsNoTracking()
                .Where(p => p.Namespace == "Template")
                .OrderBy(p => p.Name)
                .Select(p => new ApiPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description
                })
                .ToList();
        }

        public List<ApiFeatureTemplate> GetAllFeatureTemplates()
        {
            return (from p in Db.Pages.AsNoTracking()
                    join pi in Db.ProcessingInstructions.AsNoTracking() on p.Id equals pi.PageId
                    where pi.Instruction.StartsWith("template:")
                    select new ApiFeatureTemplate
                    {
                        PageId = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Type = pi.Instruction.Substring(9)
                    })
                .ToList();
        }

        public void UpdatePageProcessingInstructions(int pageId, List<string> instructions)
        {
            Db.ProcessingInstructions.Where(pi => pi.PageId == pageId).ExecuteDelete();

            var normalized = instructions.Select(i => i.ToLowerInvariant()).Distinct();
            foreach (var instruction in normalized)
            {
                Db.ProcessingInstructions.Add(new DalProcessingInstructionEntity
                {
                    PageId = pageId,
                    Instruction = instruction
                });
            }

            Db.SaveChanges();
        }

        public ApiPage? GetPageRevisionById(int pageId, int? revision = null)
        {
            var page = Db.Pages.AsNoTracking().SingleOrDefault(p => p.Id == pageId);
            if (page == null) return null;

            var effectiveRevision = revision ?? page.Revision;
            var pr = Db.PageRevisions.AsNoTracking()
                .SingleOrDefault(r => r.PageId == pageId && r.Revision == effectiveRevision);

            if (pr == null) return null;

            return MapToApiPage(page, pr, effectiveRevision);
        }

        public void SavePageSearchTokens(List<ApiPageToken> items)
        {
            var distinctItems = items.DistinctBy(i => new { i.PageId, i.Token }).ToList();

            foreach (var item in distinctItems)
            {
                var existing = Db.PageTokens.SingleOrDefault(pt => pt.PageId == item.PageId && pt.Token == item.Token);
                if (existing != null)
                {
                    existing.DoubleMetaphone = item.DoubleMetaphone;
                    existing.Weight = item.Weight;
                }
                else
                {
                    Db.PageTokens.Add(new DalPageTokenEntity
                    {
                        PageId = item.PageId,
                        Token = item.Token,
                        DoubleMetaphone = item.DoubleMetaphone,
                        Weight = item.Weight
                    });
                }
            }

            Db.SaveChanges();
        }

        public void TruncateAllPageRevisions(string confirm)
        {
            if (confirm != "YES") return;

            using var transaction = Db.Database.BeginTransaction();
            try
            {
                Db.PageRevisions.ExecuteDelete();
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
            return Db.Pages.AsNoTracking()
                .Where(p => p.Id == pageId)
                .Select(p => p.Revision)
                .SingleOrDefault();
        }

        public ApiPage? GetLimitedPageInfoByIdAndRevision(int pageId, int? revision = null)
        {
            var page = Db.Pages.AsNoTracking().SingleOrDefault(p => p.Id == pageId);
            if (page == null) return null;

            var effectiveRevision = revision ?? page.Revision;
            var pr = Db.PageRevisions.AsNoTracking()
                .SingleOrDefault(r => r.PageId == pageId && r.Revision == effectiveRevision);

            if (pr == null) return null;

            return new ApiPage
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

        public int SavePage(ApiPage page)
        {
            var navigation = NamespaceNavigation.CleanAndValidate(page.Name);
            var newDataHash = Security.Helpers.Crc32(page.Body ?? string.Empty);
            var pageNamespace = page.Name.Contains("::") ? page.Name.Substring(0, page.Name.IndexOf("::")).Trim() : string.Empty;

            using var transaction = Db.Database.BeginTransaction();
            try
            {
                int currentPageRevision = 0;
                bool hasPageChanged = false;

                if (page.Id == 0)
                {
                    var newPage = new DalPageEntity
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

                    Db.Pages.Add(newPage);
                    Db.SaveChanges();
                    page.Id = newPage.Id;
                    hasPageChanged = true;
                }
                else
                {
                    var existingPage = Db.Pages.Single(p => p.Id == page.Id);
                    var currentRevisionInfo = GetLimitedPageInfoByIdAndRevision(page.Id)
                        ?? throw new Exception("The page could not be found.");

                    currentPageRevision = currentRevisionInfo.Revision;

                    existingPage.Name = page.Name;
                    existingPage.Navigation = navigation;
                    existingPage.Namespace = pageNamespace;
                    existingPage.Description = page.Description;
                    existingPage.ModifiedByUserId = page.ModifiedByUserId;
                    existingPage.ModifiedDate = DateTime.UtcNow;
                    Db.SaveChanges();

                    hasPageChanged = currentRevisionInfo.Name != page.Name
                        || currentRevisionInfo.Description != page.Description
                        || currentRevisionInfo.ChangeSummary != page.ChangeSummary
                        || currentRevisionInfo.DataHash != newDataHash;
                }

                if (hasPageChanged)
                {
                    currentPageRevision++;

                    var pageEntity = Db.Pages.Single(p => p.Id == page.Id);
                    pageEntity.Revision = currentPageRevision;
                    Db.SaveChanges();

                    var newRevision = new DalPageRevisionEntity
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

                    Db.PageRevisions.Add(newRevision);
                    Db.SaveChanges();

                    // Reassociate attachments with new page revision
                    var currentAttachments = Db.PageRevisionAttachments
                        .Where(a => a.PageId == page.Id && a.PageRevision == currentPageRevision - 1)
                        .ToList();

                    foreach (var attachment in currentAttachments)
                    {
                        Db.PageRevisionAttachments.Add(new DAL.Models.PageRevisionAttachmentEntity
                        {
                            PageId = page.Id,
                            PageFileId = attachment.PageFileId,
                            FileRevision = attachment.FileRevision,
                            PageRevision = currentPageRevision
                        });
                    }

                    Db.SaveChanges();
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

        public ApiPage? GetPageInfoByNavigation(string navigation)
        {
            var page = Db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == navigation);
            if (page == null) return null;

            return new ApiPage
            {
                Id = page.Id,
                Name = page.Name,
                Navigation = page.Navigation,
                Description = page.Description,
                Revision = page.Revision,
                CreatedDate = page.CreatedDate,
                ModifiedDate = page.ModifiedDate
            };
        }

        public int GetPageRevisionCountByPageId(int pageId)
        {
            return Db.PageRevisions.AsNoTracking().Count(r => r.PageId == pageId);
        }

        public void RestoreDeletedPageByPageId(int pageId)
        {
            using var transaction = Db.Database.BeginTransaction();
            try
            {
                var deletedPage = Db.DeletedPages.SingleOrDefault(dp => dp.Id == pageId);
                if (deletedPage == null) return;

                var restoredPage = new DalPageEntity
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

                Db.Pages.Add(restoredPage);
                Db.DeletedPages.Remove(deletedPage);
                Db.SaveChanges();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void MovePageRevisionToDeletedById(int pageId, int revision, Guid userId)
        {
            using var transaction = Db.Database.BeginTransaction();
            try
            {
                var pageRevision = Db.PageRevisions.SingleOrDefault(pr => pr.PageId == pageId && pr.Revision == revision);
                if (pageRevision == null) return;

                var deletedRevision = new DalDeletedPageRevisionEntity
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

                Db.DeletedPageRevisions.Add(deletedRevision);
                Db.PageRevisions.Remove(pageRevision);
                Db.SaveChanges();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void MovePageToDeletedById(int pageId, Guid userId)
        {
            using var transaction = Db.Database.BeginTransaction();
            try
            {
                var page = Db.Pages.SingleOrDefault(p => p.Id == pageId);
                if (page == null) return;

                var deletedPage = new DalDeletedPageEntity
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

                Db.DeletedPages.Add(deletedPage);
                Db.Pages.Remove(page);

                // Also delete related data
                Db.PageTags.Where(t => t.PageId == pageId).ExecuteDelete();
                Db.ProcessingInstructions.Where(pi => pi.PageId == pageId).ExecuteDelete();
                Db.PageComments.Where(c => c.PageId == pageId).ExecuteDelete();
                Db.PageReferences.Where(r => r.PageId == pageId).ExecuteDelete();
                Db.PageTokens.Where(t => t.PageId == pageId).ExecuteDelete();
                Db.CompilationStatistics.Where(cs => cs.PageId == pageId).ExecuteDelete();

                Db.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void PurgeDeletedPageByPageId(int pageId)
        {
            Db.DeletedPages.Where(dp => dp.Id == pageId).ExecuteDelete();
            PurgeDeletedPageRevisionsByPageId(pageId);
        }

        public void PurgeDeletedPages()
        {
            Db.DeletedPages.ExecuteDelete();
            PurgeDeletedPageRevisions();
        }

        public int GetCountOfPageAttachmentsById(int pageId)
        {
            return Db.PageFiles.AsNoTracking().Count(pf => pf.PageId == pageId);
        }

        public ApiPage? GetDeletedPageById(int pageId)
        {
            var dp = (from deletedPage in Db.DeletedPages.AsNoTracking()
                      join profile in Db.Profiles.AsNoTracking() on deletedPage.DeletedByUserId equals profile.UserId into profiles
                      from profile in profiles.DefaultIfEmpty()
                      where deletedPage.Id == pageId
                      select new { deletedPage, profile }).SingleOrDefault();

            if (dp == null) return null;

            return new ApiPage
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

        public ApiPage? GetLatestPageRevisionById(int pageId)
        {
            var result = (from p in Db.Pages.AsNoTracking()
                          join pr in Db.PageRevisions.AsNoTracking() on new { p.Id, p.Revision } equals new { Id = pr.PageId, pr.Revision }
                          join profile in Db.Profiles.AsNoTracking() on pr.ModifiedByUserId equals profile.UserId into profiles
                          from profile in profiles.DefaultIfEmpty()
                          where p.Id == pageId
                          select new { p, pr, profile }).SingleOrDefault();

            if (result == null) return null;

            return MapToApiPage(result.p, result.pr, result.p.Revision, result.profile?.AccountName);
        }

        public int GetPageNextRevision(int pageId, int revision)
        {
            return Db.PageRevisions.AsNoTracking()
                .Where(pr => pr.PageId == pageId && pr.Revision > revision)
                .OrderBy(pr => pr.Revision)
                .Select(pr => pr.Revision)
                .FirstOrDefault();
        }

        public int GetPagePreviousRevision(int pageId, int revision)
        {
            return Db.PageRevisions.AsNoTracking()
                .Where(pr => pr.PageId == pageId && pr.Revision < revision)
                .OrderByDescending(pr => pr.Revision)
                .Select(pr => pr.Revision)
                .FirstOrDefault();
        }

        public List<ApiDeletedPageRevision> GetDeletedPageRevisionsByIdPaged(int pageId, int pageNumber,
            string? orderBy = null, string? orderByDirection = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            var query = from dpr in Db.DeletedPageRevisions.AsNoTracking()
                        join profile in Db.Profiles.AsNoTracking() on dpr.DeletedByUserId equals profile.UserId into profiles
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
                .Select(x => new ApiDeletedPageRevision
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
            Db.DeletedPageRevisions.ExecuteDelete();
        }

        public void PurgeDeletedPageRevisionsByPageId(int pageId)
        {
            Db.DeletedPageRevisions.Where(dpr => dpr.PageId == pageId).ExecuteDelete();
        }

        public void PurgeDeletedPageRevisionByPageIdAndRevision(int pageId, int revision)
        {
            Db.DeletedPageRevisions
                .Where(dpr => dpr.PageId == pageId && dpr.Revision == revision)
                .ExecuteDelete();
        }

        public void RestoreDeletedPageRevisionByPageIdAndRevision(int pageId, int revision)
        {
            var deletedRevision = Db.DeletedPageRevisions
                .SingleOrDefault(dpr => dpr.PageId == pageId && dpr.Revision == revision);

            if (deletedRevision == null) return;

            var restoredRevision = new DalPageRevisionEntity
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

            Db.PageRevisions.Add(restoredRevision);
            Db.DeletedPageRevisions.Remove(deletedRevision);
            Db.SaveChanges();
        }

        public ApiDeletedPageRevision? GetDeletedPageRevisionById(int pageId, int revision)
        {
            var result = (from dpr in Db.DeletedPageRevisions.AsNoTracking()
                          join profile in Db.Profiles.AsNoTracking() on dpr.DeletedByUserId equals profile.UserId into profiles
                          from profile in profiles.DefaultIfEmpty()
                          where dpr.PageId == pageId && dpr.Revision == revision
                          select new { dpr, profile }).SingleOrDefault();

            if (result == null) return null;

            return new ApiDeletedPageRevision
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

        public ApiPage? GetPageRevisionByNavigation(NamespaceNavigation navigation, int? revision = null)
        {
            return GetPageRevisionByNavigation(navigation.Canonical, revision, false);
        }

        public ApiPage? GetPageRevisionByNavigation(string givenNavigation, int? revision = null, bool refreshCache = false)
        {
            var navigation = new NamespaceNavigation(givenNavigation);

            var page = Db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == navigation.Canonical);
            if (page == null) return null;

            var effectiveRevision = revision ?? page.Revision;

            var result = (from pr in Db.PageRevisions.AsNoTracking()
                          join profile in Db.Profiles.AsNoTracking() on pr.ModifiedByUserId equals profile.UserId into profiles
                          from profile in profiles.DefaultIfEmpty()
                          where pr.PageId == page.Id && pr.Revision == effectiveRevision
                          select new { pr, profile }).SingleOrDefault();

            if (result == null) return null;

            return MapToApiPage(page, result.pr, effectiveRevision, result.profile?.AccountName);
        }

        public List<ApiTagAssociation> GetAssociatedTags(string tag)
        {
            return (from pt in Db.PageTags.AsNoTracking()
                    join p in Db.Pages.AsNoTracking() on pt.PageId equals p.Id
                    where Db.PageTags.Any(t => t.PageId == p.Id && t.Tag == tag) && pt.Tag != tag
                    group pt by pt.Tag into g
                    select new ApiTagAssociation
                    {
                        Tag = g.Key,
                        PageCount = g.Count()
                    })
                .OrderByDescending(t => t.PageCount)
                .ToList();
        }

        public List<ApiPage> GetPageInfoByNamespaces(List<string> namespaces)
        {
            return Db.Pages.AsNoTracking()
                .Where(p => namespaces.Contains(p.Namespace))
                .Select(p => new ApiPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description
                })
                .ToList();
        }

        public List<ApiPage> GetPageInfoByTags(List<string> tags)
        {
            var pageIds = Db.PageTags.AsNoTracking()
                .Where(t => tags.Contains(t.Tag))
                .Select(t => t.PageId)
                .Distinct()
                .ToList();

            return Db.Pages.AsNoTracking()
                .Where(p => pageIds.Contains(p.Id))
                .Select(p => new ApiPage
                {
                    Id = p.Id,
                    Name = p.Name,
                    Navigation = p.Navigation,
                    Description = p.Description
                })
                .ToList();
        }

        public List<ApiPage> GetPageInfoByTag(string tag)
        {
            return GetPageInfoByTags([tag]);
        }

        public void UpdatePageTags(int pageId, List<string> tags)
        {
            Db.PageTags.Where(t => t.PageId == pageId).ExecuteDelete();

            var normalized = tags.Select(t => t.ToLowerInvariant()).Distinct();
            foreach (var tag in normalized)
            {
                Db.PageTags.Add(new DalPageTagEntity
                {
                    PageId = pageId,
                    Tag = tag
                });
            }

            Db.SaveChanges();
        }

        private static ApiPage MapToApiPage(DalPageEntity page, DalPageRevisionEntity revision, int currentRevision, string? modifiedByUserName = null)
        {
            return new ApiPage
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
    }
}
