using DAL;
using Microsoft.EntityFrameworkCore;
using NTDLS.Helpers;
using TightWiki.Caching;
using TightWiki.Models;
using DalPageEntity = DAL.Models.PageEntity;
using DalPageFileEntity = DAL.Models.PageFileEntity;
using DalPageFileRevisionEntity = DAL.Models.PageFileRevisionEntity;
using DalPageRevisionAttachmentEntity = DAL.Models.PageRevisionAttachmentEntity;
using ApiOrphanedPageAttachment = TightWiki.Models.DataModels.OrphanedPageAttachment;
using ApiPageFileAttachment = TightWiki.Models.DataModels.PageFileAttachment;
using ApiPageFileAttachmentInfo = TightWiki.Models.DataModels.PageFileAttachmentInfo;
using ApiPageFileRevisionAttachmentInfo = TightWiki.Models.DataModels.PageFileRevisionAttachmentInfo;

namespace TightWiki.Repository
{
    public interface IPageFileRepository
    {
        void DetachPageRevisionAttachment(string pageNavigation, string fileNavigation, int pageRevision);
        List<ApiOrphanedPageAttachment> GetOrphanedPageAttachmentsPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null);
        void PurgeOrphanedPageAttachments();
        void PurgeOrphanedPageAttachment(int pageFileId, int revision);
        List<ApiPageFileAttachmentInfo> GetPageFilesInfoByPageNavigationAndPageRevisionPaged(string pageNavigation, int pageNumber, int? pageSize = null, int? pageRevision = null);
        ApiPageFileAttachmentInfo? GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null);
        ApiPageFileAttachment? GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? fileRevision = null);
        ApiPageFileAttachment? GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null);
        List<ApiPageFileAttachmentInfo> GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged(string pageNavigation, string fileNavigation, int pageNumber);
        List<ApiPageFileAttachmentInfo> GetPageFilesInfoByPageId(int pageId);
        ApiPageFileRevisionAttachmentInfo? GetPageFileInfoByFileNavigation(int pageId, string fileNavigation);
        ApiPageFileRevisionAttachmentInfo? GetPageCurrentRevisionAttachmentByFileNavigation(int pageId, string fileNavigation);
        void UpsertPageFile(ApiPageFileAttachment item, Guid userId);
        int GetCurrentPageRevision(int pageId);
    }

    public static class PageFileRepository
    {
        private static IServiceProvider? _serviceProvider;
        public static void UseServiceProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        private static IPageFileRepository Repo =>
            _serviceProvider?.GetService(typeof(IPageFileRepository)) as IPageFileRepository
            ?? throw new InvalidOperationException("IPageFileRepository is not configured.");

        public static void DetachPageRevisionAttachment(string pageNavigation, string fileNavigation, int pageRevision)
            => Repo.DetachPageRevisionAttachment(pageNavigation, fileNavigation, pageRevision);

        public static List<ApiOrphanedPageAttachment> GetOrphanedPageAttachmentsPaged(
            int pageNumber, string? orderBy = null, string? orderByDirection = null)
            => Repo.GetOrphanedPageAttachmentsPaged(pageNumber, orderBy, orderByDirection);

        public static void PurgeOrphanedPageAttachments()
            => Repo.PurgeOrphanedPageAttachments();

        public static void PurgeOrphanedPageAttachment(int pageFileId, int revision)
            => Repo.PurgeOrphanedPageAttachment(pageFileId, revision);

        public static List<ApiPageFileAttachmentInfo> GetPageFilesInfoByPageNavigationAndPageRevisionPaged(
            string pageNavigation, int pageNumber, int? pageSize = null, int? pageRevision = null)
            => Repo.GetPageFilesInfoByPageNavigationAndPageRevisionPaged(pageNavigation, pageNumber, pageSize, pageRevision);

        public static ApiPageFileAttachmentInfo? GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(
            string pageNavigation, string fileNavigation, int? pageRevision = null)
            => Repo.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(pageNavigation, fileNavigation, pageRevision);

        public static ApiPageFileAttachment? GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(
            string pageNavigation, string fileNavigation, int? fileRevision = null)
            => Repo.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(pageNavigation, fileNavigation, fileRevision);

        public static ApiPageFileAttachment? GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(
            string pageNavigation, string fileNavigation, int? pageRevision = null)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [pageNavigation, fileNavigation, pageRevision]);
            return WikiCache.AddOrGet(cacheKey, () =>
                Repo.GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(pageNavigation, fileNavigation, pageRevision));
        }

        public static List<ApiPageFileAttachmentInfo> GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged(
            string pageNavigation, string fileNavigation, int pageNumber)
            => Repo.GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged(pageNavigation, fileNavigation, pageNumber);

        public static List<ApiPageFileAttachmentInfo> GetPageFilesInfoByPageId(int pageId)
            => Repo.GetPageFilesInfoByPageId(pageId);

        public static void UpsertPageFile(ApiPageFileAttachment item, Guid userId)
            => Repo.UpsertPageFile(item, userId);
    }

    public sealed class PageFileRepositoryEf : IPageFileRepository
    {
        public WikiDbContext Db { get; }

        public PageFileRepositoryEf(WikiDbContext db)
        {
            Db = db;
        }

        public void DetachPageRevisionAttachment(string pageNavigation, string fileNavigation, int pageRevision)
        {
            var page = Db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == pageNavigation);
            if (page == null) return;

            var pageFile = Db.PageFiles.AsNoTracking().SingleOrDefault(f => f.PageId == page.Id && f.Navigation == fileNavigation);
            if (pageFile == null) return;

            var attachments = Db.PageRevisionAttachments
                .Where(a => a.PageId == page.Id && a.PageFileId == pageFile.Id && a.PageRevision == pageRevision);

            Db.PageRevisionAttachments.RemoveRange(attachments);
            Db.SaveChanges();
        }

        public List<ApiOrphanedPageAttachment> GetOrphanedPageAttachmentsPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            // Orphaned attachments are file revisions not linked to any page revision
            var orphanedQuery = from pfr in Db.PageFileRevisions.AsNoTracking()
                                join pf in Db.PageFiles.AsNoTracking() on pfr.PageFileId equals pf.Id
                                join p in Db.Pages.AsNoTracking() on pf.PageId equals p.Id
                                where !Db.PageRevisionAttachments.Any(pra =>
                                    pra.PageFileId == pfr.PageFileId && pra.FileRevision == pfr.Revision)
                                select new { pfr, pf, p };

            var totalCount = orphanedQuery.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

            var isAsc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);
            orphanedQuery = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "pagename" => isAsc ? orphanedQuery.OrderBy(x => x.p.Name) : orphanedQuery.OrderByDescending(x => x.p.Name),
                "filename" => isAsc ? orphanedQuery.OrderBy(x => x.pf.Name) : orphanedQuery.OrderByDescending(x => x.pf.Name),
                "size" => isAsc ? orphanedQuery.OrderBy(x => x.pfr.Size) : orphanedQuery.OrderByDescending(x => x.pfr.Size),
                _ => orphanedQuery.OrderBy(x => x.pf.Name)
            };

            return orphanedQuery
                .Skip(skip)
                .Take(pageSize)
                .Select(x => new ApiOrphanedPageAttachment
                {
                    PageFileId = x.pf.Id,
                    PageName = x.p.Name,
                    Namespace = x.p.Namespace,
                    PageNavigation = x.p.Navigation,
                    FileName = x.pf.Name,
                    FileNavigation = x.pf.Navigation,
                    Size = x.pfr.Size,
                    FileRevision = x.pfr.Revision,
                    PaginationPageCount = pageCount
                })
                .ToList();
        }

        public void PurgeOrphanedPageAttachments()
        {
            // Find orphaned file revisions (not linked to any page revision)
            var orphanedRevisionIds = Db.PageFileRevisions.AsNoTracking()
                .Where(pfr => !Db.PageRevisionAttachments.Any(pra =>
                    pra.PageFileId == pfr.PageFileId && pra.FileRevision == pfr.Revision))
                .Select(pfr => pfr.Id)
                .ToList();

            if (orphanedRevisionIds.Count > 0)
            {
                Db.PageFileRevisions
                    .Where(pfr => orphanedRevisionIds.Contains(pfr.Id))
                    .ExecuteDelete();
            }
        }

        public void PurgeOrphanedPageAttachment(int pageFileId, int revision)
        {
            var entity = Db.PageFileRevisions
                .SingleOrDefault(pfr => pfr.PageFileId == pageFileId && pfr.Revision == revision);

            if (entity != null)
            {
                Db.PageFileRevisions.Remove(entity);
                Db.SaveChanges();
            }
        }

        public List<ApiPageFileAttachmentInfo> GetPageFilesInfoByPageNavigationAndPageRevisionPaged(
            string pageNavigation, int pageNumber, int? pageSize = null, int? pageRevision = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize.Value;

            var page = Db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == pageNavigation);
            if (page == null) return [];

            var effectivePageRevision = pageRevision ?? page.Revision;

            var query = from pf in Db.PageFiles.AsNoTracking()
                        join pra in Db.PageRevisionAttachments.AsNoTracking()
                            on new { pf.PageId, PageFileId = pf.Id, FileRevision = pf.Revision }
                            equals new { pra.PageId, pra.PageFileId, pra.FileRevision }
                        join pfr in Db.PageFileRevisions.AsNoTracking()
                            on new { pra.PageFileId, Revision = pra.FileRevision }
                            equals new { pfr.PageFileId, pfr.Revision }
                        join profile in Db.Profiles.AsNoTracking() on pfr.CreatedByUserId equals profile.UserId into profiles
                        from profile in profiles.DefaultIfEmpty()
                        where pf.PageId == page.Id && pra.PageRevision == effectivePageRevision
                        select new { pf, pfr, profile };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

            return query
                .OrderBy(x => x.pf.Name)
                .Skip(skip)
                .Take(pageSize.Value)
                .Select(x => new ApiPageFileAttachmentInfo
                {
                    Id = x.pf.Id,
                    PageId = x.pf.PageId,
                    Name = x.pf.Name,
                    ContentType = x.pfr.ContentType,
                    Size = x.pfr.Size,
                    CreatedDate = x.pf.CreatedDate,
                    FileNavigation = x.pf.Navigation,
                    PageNavigation = pageNavigation,
                    FileRevision = x.pfr.Revision,
                    CreatedByUserId = x.pfr.CreatedByUserId,
                    CreatedByUserName = x.profile != null ? x.profile.AccountName : string.Empty,
                    CreatedByNavigation = x.profile != null ? x.profile.Navigation : string.Empty,
                    PaginationPageSize = pageSize.Value,
                    PaginationPageCount = pageCount
                })
                .ToList();
        }

        public ApiPageFileAttachmentInfo? GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(
            string pageNavigation, string fileNavigation, int? pageRevision = null)
        {
            var page = Db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == pageNavigation);
            if (page == null) return null;

            var effectivePageRevision = pageRevision ?? page.Revision;

            var result = (from pf in Db.PageFiles.AsNoTracking()
                          join pra in Db.PageRevisionAttachments.AsNoTracking()
                              on new { pf.PageId, PageFileId = pf.Id }
                              equals new { pra.PageId, pra.PageFileId }
                          join pfr in Db.PageFileRevisions.AsNoTracking()
                              on new { pra.PageFileId, Revision = pra.FileRevision }
                              equals new { pfr.PageFileId, pfr.Revision }
                          join profile in Db.Profiles.AsNoTracking() on pfr.CreatedByUserId equals profile.UserId into profiles
                          from profile in profiles.DefaultIfEmpty()
                          where pf.PageId == page.Id
                                && pf.Navigation == fileNavigation
                                && pra.PageRevision == effectivePageRevision
                          select new ApiPageFileAttachmentInfo
                          {
                              Id = pf.Id,
                              PageId = pf.PageId,
                              Name = pf.Name,
                              ContentType = pfr.ContentType,
                              Size = pfr.Size,
                              CreatedDate = pf.CreatedDate,
                              FileNavigation = pf.Navigation,
                              PageNavigation = pageNavigation,
                              FileRevision = pfr.Revision,
                              CreatedByUserId = pfr.CreatedByUserId,
                              CreatedByUserName = profile != null ? profile.AccountName : string.Empty,
                              CreatedByNavigation = profile != null ? profile.Navigation : string.Empty
                          }).SingleOrDefault();

            return result;
        }

        public ApiPageFileAttachment? GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(
            string pageNavigation, string fileNavigation, int? fileRevision = null)
        {
            var page = Db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == pageNavigation);
            if (page == null) return null;

            var pageFile = Db.PageFiles.AsNoTracking()
                .SingleOrDefault(pf => pf.PageId == page.Id && pf.Navigation == fileNavigation);
            if (pageFile == null) return null;

            var effectiveFileRevision = fileRevision ?? pageFile.Revision;

            var pfr = Db.PageFileRevisions.AsNoTracking()
                .SingleOrDefault(r => r.PageFileId == pageFile.Id && r.Revision == effectiveFileRevision);
            if (pfr == null) return null;

            return new ApiPageFileAttachment
            {
                Id = pageFile.Id,
                PageId = pageFile.PageId,
                Name = pageFile.Name,
                ContentType = pfr.ContentType,
                Size = pfr.Size,
                CreatedDate = pageFile.CreatedDate,
                Data = pfr.Data,
                FileNavigation = pageFile.Navigation,
                PageNavigation = pageNavigation
            };
        }

        public ApiPageFileAttachment? GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(
            string pageNavigation, string fileNavigation, int? pageRevision = null)
        {
            var page = Db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == pageNavigation);
            if (page == null) return null;

            var effectivePageRevision = pageRevision ?? page.Revision;

            var result = (from pf in Db.PageFiles.AsNoTracking()
                          join pra in Db.PageRevisionAttachments.AsNoTracking()
                              on new { pf.PageId, PageFileId = pf.Id }
                              equals new { pra.PageId, pra.PageFileId }
                          join pfr in Db.PageFileRevisions.AsNoTracking()
                              on new { pra.PageFileId, Revision = pra.FileRevision }
                              equals new { pfr.PageFileId, pfr.Revision }
                          where pf.PageId == page.Id
                                && pf.Navigation == fileNavigation
                                && pra.PageRevision == effectivePageRevision
                          select new ApiPageFileAttachment
                          {
                              Id = pf.Id,
                              PageId = pf.PageId,
                              Name = pf.Name,
                              ContentType = pfr.ContentType,
                              Size = pfr.Size,
                              CreatedDate = pf.CreatedDate,
                              Data = pfr.Data,
                              FileNavigation = pf.Navigation,
                              PageNavigation = pageNavigation
                          }).SingleOrDefault();

            return result;
        }

        public List<ApiPageFileAttachmentInfo> GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged(
            string pageNavigation, string fileNavigation, int pageNumber)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            var page = Db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == pageNavigation);
            if (page == null) return [];

            var pageFile = Db.PageFiles.AsNoTracking()
                .SingleOrDefault(pf => pf.PageId == page.Id && pf.Navigation == fileNavigation);
            if (pageFile == null) return [];

            var query = from pfr in Db.PageFileRevisions.AsNoTracking()
                        join profile in Db.Profiles.AsNoTracking() on pfr.CreatedByUserId equals profile.UserId into profiles
                        from profile in profiles.DefaultIfEmpty()
                        where pfr.PageFileId == pageFile.Id
                        select new { pfr, profile };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

            return query
                .OrderByDescending(x => x.pfr.Revision)
                .Skip(skip)
                .Take(pageSize)
                .Select(x => new ApiPageFileAttachmentInfo
                {
                    Id = pageFile.Id,
                    PageId = pageFile.PageId,
                    Name = pageFile.Name,
                    ContentType = x.pfr.ContentType,
                    Size = x.pfr.Size,
                    CreatedDate = x.pfr.CreatedDate,
                    FileNavigation = pageFile.Navigation,
                    PageNavigation = pageNavigation,
                    FileRevision = x.pfr.Revision,
                    CreatedByUserId = x.pfr.CreatedByUserId,
                    CreatedByUserName = x.profile != null ? x.profile.AccountName : string.Empty,
                    CreatedByNavigation = x.profile != null ? x.profile.Navigation : string.Empty,
                    PaginationPageSize = pageSize,
                    PaginationPageCount = pageCount
                })
                .ToList();
        }

        public List<ApiPageFileAttachmentInfo> GetPageFilesInfoByPageId(int pageId)
        {
            var page = Db.Pages.AsNoTracking().SingleOrDefault(p => p.Id == pageId);
            if (page == null) return [];

            return (from pf in Db.PageFiles.AsNoTracking()
                    join pfr in Db.PageFileRevisions.AsNoTracking()
                        on new { PageFileId = pf.Id, pf.Revision }
                        equals new { pfr.PageFileId, pfr.Revision }
                    where pf.PageId == pageId
                    select new ApiPageFileAttachmentInfo
                    {
                        Id = pf.Id,
                        PageId = pf.PageId,
                        Name = pf.Name,
                        ContentType = pfr.ContentType,
                        Size = pfr.Size,
                        CreatedDate = pf.CreatedDate,
                        FileNavigation = pf.Navigation,
                        PageNavigation = page.Navigation,
                        FileRevision = pfr.Revision
                    }).ToList();
        }

        public ApiPageFileRevisionAttachmentInfo? GetPageFileInfoByFileNavigation(int pageId, string fileNavigation)
        {
            var pageFile = Db.PageFiles.AsNoTracking()
                .SingleOrDefault(pf => pf.PageId == pageId && pf.Navigation == fileNavigation);

            if (pageFile == null) return null;

            var pfr = Db.PageFileRevisions.AsNoTracking()
                .SingleOrDefault(r => r.PageFileId == pageFile.Id && r.Revision == pageFile.Revision);

            return new ApiPageFileRevisionAttachmentInfo
            {
                PageId = pageId,
                PageFileId = pageFile.Id,
                Revision = pageFile.Revision,
                ContentType = pfr?.ContentType ?? string.Empty,
                Size = (int)(pfr?.Size ?? 0),
                DataHash = pfr?.DataHash ?? 0
            };
        }

        public ApiPageFileRevisionAttachmentInfo? GetPageCurrentRevisionAttachmentByFileNavigation(int pageId, string fileNavigation)
        {
            var page = Db.Pages.AsNoTracking().SingleOrDefault(p => p.Id == pageId);
            if (page == null) return null;

            var pageFile = Db.PageFiles.AsNoTracking()
                .SingleOrDefault(pf => pf.PageId == pageId && pf.Navigation == fileNavigation);
            if (pageFile == null) return null;

            var pra = Db.PageRevisionAttachments.AsNoTracking()
                .SingleOrDefault(a => a.PageId == pageId
                    && a.PageFileId == pageFile.Id
                    && a.PageRevision == page.Revision);

            if (pra == null) return null;

            var pfr = Db.PageFileRevisions.AsNoTracking()
                .SingleOrDefault(r => r.PageFileId == pageFile.Id && r.Revision == pra.FileRevision);

            return new ApiPageFileRevisionAttachmentInfo
            {
                PageId = pageId,
                PageFileId = pageFile.Id,
                Revision = pra.FileRevision,
                ContentType = pfr?.ContentType ?? string.Empty,
                Size = (int)(pfr?.Size ?? 0),
                DataHash = pfr?.DataHash ?? 0
            };
        }

        public int GetCurrentPageRevision(int pageId)
        {
            return Db.Pages.AsNoTracking()
                .Where(p => p.Id == pageId)
                .Select(p => p.Revision)
                .SingleOrDefault();
        }

        public void UpsertPageFile(ApiPageFileAttachment item, Guid userId)
        {
            using var transaction = Db.Database.BeginTransaction();

            try
            {
                var pageFileInfo = GetPageFileInfoByFileNavigation(item.PageId, item.FileNavigation);
                bool hasFileChanged = false;
                int pageFileId;
                int currentFileRevision = 0;

                if (pageFileInfo == null)
                {
                    // Insert new page file
                    var newPageFile = new DalPageFileEntity
                    {
                        PageId = item.PageId,
                        Name = item.Name,
                        Navigation = item.FileNavigation,
                        CreatedDate = item.CreatedDate,
                        Revision = 0
                    };

                    Db.PageFiles.Add(newPageFile);
                    Db.SaveChanges();

                    pageFileId = newPageFile.Id;
                    hasFileChanged = true;
                }
                else
                {
                    pageFileId = pageFileInfo.PageFileId;
                    currentFileRevision = pageFileInfo.Revision;
                }

                var newDataHash = Security.Helpers.Crc32(item.Data);

                var currentlyAttachedFile = GetPageCurrentRevisionAttachmentByFileNavigation(item.PageId, item.FileNavigation);
                if (currentlyAttachedFile != null)
                {
                    currentFileRevision = currentlyAttachedFile.Revision;
                    hasFileChanged = currentlyAttachedFile.DataHash != newDataHash;
                }
                else
                {
                    hasFileChanged = true;
                    if (pageFileInfo != null)
                    {
                        currentFileRevision = pageFileInfo.Revision;
                    }
                }

                if (hasFileChanged)
                {
                    currentFileRevision++;
                    int currentPageRevision = GetCurrentPageRevision(item.PageId);

                    // Update page file revision number
                    var pageFile = Db.PageFiles.Single(pf => pf.Id == pageFileId);
                    pageFile.Revision = currentFileRevision;
                    Db.SaveChanges();

                    // Insert new file revision
                    var newRevision = new DalPageFileRevisionEntity
                    {
                        PageFileId = pageFileId,
                        ContentType = item.ContentType,
                        Size = item.Size,
                        CreatedDate = item.CreatedDate,
                        CreatedByUserId = userId,
                        Data = item.Data,
                        Revision = currentFileRevision,
                        DataHash = newDataHash
                    };

                    Db.PageFileRevisions.Add(newRevision);
                    Db.SaveChanges();

                    // Remove previous attachment if exists
                    if (currentlyAttachedFile != null)
                    {
                        var prevAttachment = Db.PageRevisionAttachments
                            .SingleOrDefault(a => a.PageId == item.PageId
                                && a.PageFileId == pageFileId
                                && a.FileRevision == currentlyAttachedFile.Revision
                                && a.PageRevision == currentPageRevision);

                        if (prevAttachment != null)
                        {
                            Db.PageRevisionAttachments.Remove(prevAttachment);
                        }
                    }

                    // Associate file revision with page revision
                    var newAttachment = new DalPageRevisionAttachmentEntity
                    {
                        PageId = item.PageId,
                        PageFileId = pageFileId,
                        FileRevision = currentFileRevision,
                        PageRevision = currentPageRevision
                    };

                    Db.PageRevisionAttachments.Add(newAttachment);
                    Db.SaveChanges();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
