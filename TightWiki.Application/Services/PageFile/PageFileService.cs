using TightWiki.Contracts;
using TightWiki.Contracts.DataModels;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using TightWiki.Utils.Caching;
using TightWiki.Utils.Security;

namespace BLL.Services.PageFile
{
    /// <summary>
    /// Business logic service for page file attachment operations.
    /// </summary>
    public sealed class PageFileService : IPageFileService
    {
        private readonly WikiDbContext _db;

        public PageFileService(WikiDbContext db)
        {
            _db = db;
        }

        public void DetachPageRevisionAttachment(string pageNavigation, string fileNavigation, int pageRevision)
        {
            var page = _db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == pageNavigation);
            if (page == null) return;

            var pageFile = _db.PageFiles.AsNoTracking().SingleOrDefault(f => f.PageId == page.Id && f.Navigation == fileNavigation);
            if (pageFile == null) return;

            var attachments = _db.PageRevisionAttachments
                .Where(a => a.PageId == page.Id && a.PageFileId == pageFile.Id && a.PageRevision == pageRevision);

            _db.PageRevisionAttachments.RemoveRange(attachments);
            _db.SaveChanges();

            WikiCache.ClearCategory(WikiCache.Category.Page);
        }

        public List<OrphanedPageAttachment> GetOrphanedPageAttachmentsPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            // Orphaned attachments are file revisions not linked to any page revision
            var orphanedQuery = from pfr in _db.PageFileRevisions.AsNoTracking()
                                join pf in _db.PageFiles.AsNoTracking() on pfr.PageFileId equals pf.Id
                                join p in _db.Pages.AsNoTracking() on pf.PageId equals p.Id
                                where !_db.PageRevisionAttachments.Any(pra =>
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
                .Select(x => new OrphanedPageAttachment
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
            var orphanedRevisionIds = _db.PageFileRevisions.AsNoTracking()
                .Where(pfr => !_db.PageRevisionAttachments.Any(pra =>
                    pra.PageFileId == pfr.PageFileId && pra.FileRevision == pfr.Revision))
                .Select(pfr => pfr.Id)
                .ToList();

            if (orphanedRevisionIds.Count > 0)
            {
                _db.PageFileRevisions
                    .Where(pfr => orphanedRevisionIds.Contains(pfr.Id))
                    .ExecuteDelete();
            }

            WikiCache.ClearCategory(WikiCache.Category.Page);
        }

        public void PurgeOrphanedPageAttachment(int pageFileId, int revision)
        {
            var entity = _db.PageFileRevisions
                .SingleOrDefault(pfr => pfr.PageFileId == pageFileId && pfr.Revision == revision);

            if (entity != null)
            {
                _db.PageFileRevisions.Remove(entity);
                _db.SaveChanges();
            }

            WikiCache.ClearCategory(WikiCache.Category.Page);
        }

        public List<PageFileAttachmentInfo> GetPageFilesInfoByPageNavigationAndPageRevisionPaged(
            string pageNavigation, int pageNumber, int? pageSize = null, int? pageRevision = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize.Value;

            var page = _db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == pageNavigation);
            if (page == null) return [];

            var effectivePageRevision = pageRevision ?? page.Revision;

            var query = from pf in _db.PageFiles.AsNoTracking()
                        join pra in _db.PageRevisionAttachments.AsNoTracking()
                            on new { pf.PageId, PageFileId = pf.Id, FileRevision = pf.Revision }
                            equals new { pra.PageId, pra.PageFileId, pra.FileRevision }
                        join pfr in _db.PageFileRevisions.AsNoTracking()
                            on new { pra.PageFileId, Revision = pra.FileRevision }
                            equals new { pfr.PageFileId, pfr.Revision }
                        join profile in _db.Profiles.AsNoTracking() on pfr.CreatedByUserId equals profile.UserId into profiles
                        from profile in profiles.DefaultIfEmpty()
                        where pf.PageId == page.Id && pra.PageRevision == effectivePageRevision
                        select new { pf, pfr, profile };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

            return query
                .OrderBy(x => x.pf.Name)
                .Skip(skip)
                .Take(pageSize.Value)
                .Select(x => new PageFileAttachmentInfo
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

        public PageFileAttachmentInfo? GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(
            string pageNavigation, string fileNavigation, int? pageRevision = null)
        {
            var page = _db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == pageNavigation);
            if (page == null) return null;

            var effectivePageRevision = pageRevision ?? page.Revision;

            var result = (from pf in _db.PageFiles.AsNoTracking()
                          join pra in _db.PageRevisionAttachments.AsNoTracking()
                              on new { pf.PageId, PageFileId = pf.Id }
                              equals new { pra.PageId, pra.PageFileId }
                          join pfr in _db.PageFileRevisions.AsNoTracking()
                              on new { pra.PageFileId, Revision = pra.FileRevision }
                              equals new { pfr.PageFileId, pfr.Revision }
                          join profile in _db.Profiles.AsNoTracking() on pfr.CreatedByUserId equals profile.UserId into profiles
                          from profile in profiles.DefaultIfEmpty()
                          where pf.PageId == page.Id
                                && pf.Navigation == fileNavigation
                                && pra.PageRevision == effectivePageRevision
                          select new PageFileAttachmentInfo
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

        public PageFileAttachment? GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(
            string pageNavigation, string fileNavigation, int? fileRevision = null)
        {
            var page = _db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == pageNavigation);
            if (page == null) return null;

            var pageFile = _db.PageFiles.AsNoTracking()
                .SingleOrDefault(pf => pf.PageId == page.Id && pf.Navigation == fileNavigation);
            if (pageFile == null) return null;

            var effectiveFileRevision = fileRevision ?? pageFile.Revision;

            var pfr = _db.PageFileRevisions.AsNoTracking()
                .SingleOrDefault(r => r.PageFileId == pageFile.Id && r.Revision == effectiveFileRevision);
            if (pfr == null) return null;

            return new PageFileAttachment
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

        public PageFileAttachment? GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(
            string pageNavigation, string fileNavigation, int? pageRevision = null)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [pageNavigation, fileNavigation, pageRevision]);
            return WikiCache.AddOrGet(cacheKey, () =>
            {
                var page = _db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == pageNavigation);
                if (page == null) return null;

                var effectivePageRevision = pageRevision ?? page.Revision;

                var result = (from pf in _db.PageFiles.AsNoTracking()
                              join pra in _db.PageRevisionAttachments.AsNoTracking()
                                  on new { pf.PageId, PageFileId = pf.Id }
                                  equals new { pra.PageId, pra.PageFileId }
                              join pfr in _db.PageFileRevisions.AsNoTracking()
                                  on new { pra.PageFileId, Revision = pra.FileRevision }
                                  equals new { pfr.PageFileId, pfr.Revision }
                              where pf.PageId == page.Id
                                    && pf.Navigation == fileNavigation
                                    && pra.PageRevision == effectivePageRevision
                              select new PageFileAttachment
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
            });
        }

        public List<PageFileAttachmentInfo> GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged(
            string pageNavigation, string fileNavigation, int pageNumber)
        {
            var pageSize = GlobalConfiguration.PaginationSize;
            var skip = (pageNumber - 1) * pageSize;

            var page = _db.Pages.AsNoTracking().SingleOrDefault(p => p.Navigation == pageNavigation);
            if (page == null) return [];

            var pageFile = _db.PageFiles.AsNoTracking()
                .SingleOrDefault(pf => pf.PageId == page.Id && pf.Navigation == fileNavigation);
            if (pageFile == null) return [];

            var query = from pfr in _db.PageFileRevisions.AsNoTracking()
                        join profile in _db.Profiles.AsNoTracking() on pfr.CreatedByUserId equals profile.UserId into profiles
                        from profile in profiles.DefaultIfEmpty()
                        where pfr.PageFileId == pageFile.Id
                        select new { pfr, profile };

            var totalCount = query.Count();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

            return query
                .OrderByDescending(x => x.pfr.Revision)
                .Skip(skip)
                .Take(pageSize)
                .Select(x => new PageFileAttachmentInfo
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

        public List<PageFileAttachmentInfo> GetPageFilesInfoByPageId(int pageId)
        {
            var page = _db.Pages.AsNoTracking().SingleOrDefault(p => p.Id == pageId);
            if (page == null) return [];

            return (from pf in _db.PageFiles.AsNoTracking()
                    join pfr in _db.PageFileRevisions.AsNoTracking()
                        on new { PageFileId = pf.Id, pf.Revision }
                        equals new { pfr.PageFileId, pfr.Revision }
                    where pf.PageId == pageId
                    select new PageFileAttachmentInfo
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

        public PageFileRevisionAttachmentInfo? GetPageFileInfoByFileNavigation(int pageId, string fileNavigation)
        {
            var pageFile = _db.PageFiles.AsNoTracking()
                .SingleOrDefault(pf => pf.PageId == pageId && pf.Navigation == fileNavigation);

            if (pageFile == null) return null;

            var pfr = _db.PageFileRevisions.AsNoTracking()
                .SingleOrDefault(r => r.PageFileId == pageFile.Id && r.Revision == pageFile.Revision);

            return new PageFileRevisionAttachmentInfo
            {
                PageId = pageId,
                PageFileId = pageFile.Id,
                Revision = pageFile.Revision,
                ContentType = pfr?.ContentType ?? string.Empty,
                Size = (int)(pfr?.Size ?? 0),
                DataHash = pfr?.DataHash ?? 0
            };
        }

        public PageFileRevisionAttachmentInfo? GetPageCurrentRevisionAttachmentByFileNavigation(int pageId, string fileNavigation)
        {
            var page = _db.Pages.AsNoTracking().SingleOrDefault(p => p.Id == pageId);
            if (page == null) return null;

            var pageFile = _db.PageFiles.AsNoTracking()
                .SingleOrDefault(pf => pf.PageId == pageId && pf.Navigation == fileNavigation);
            if (pageFile == null) return null;

            var pra = _db.PageRevisionAttachments.AsNoTracking()
                .SingleOrDefault(a => a.PageId == pageId
                    && a.PageFileId == pageFile.Id
                    && a.PageRevision == page.Revision);

            if (pra == null) return null;

            var pfr = _db.PageFileRevisions.AsNoTracking()
                .SingleOrDefault(r => r.PageFileId == pageFile.Id && r.Revision == pra.FileRevision);

            return new PageFileRevisionAttachmentInfo
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
            return _db.Pages.AsNoTracking()
                .Where(p => p.Id == pageId)
                .Select(p => p.Revision)
                .SingleOrDefault();
        }

        public void UpsertPageFile(PageFileAttachment item, Guid userId)
        {
            using var transaction = _db.Database.BeginTransaction();

            try
            {
                var pageFileInfo = GetPageFileInfoByFileNavigation(item.PageId, item.FileNavigation);
                bool hasFileChanged = false;
                int pageFileId;
                int currentFileRevision = 0;

                if (pageFileInfo == null)
                {
                    // Insert new page file
                    var newPageFile = new PageFileEntityDB
                    {
                        PageId = item.PageId,
                        Name = item.Name,
                        Navigation = item.FileNavigation,
                        CreatedDate = item.CreatedDate,
                        Revision = 0
                    };

                    _db.PageFiles.Add(newPageFile);
                    _db.SaveChanges();

                    pageFileId = newPageFile.Id;
                    hasFileChanged = true;
                }
                else
                {
                    pageFileId = pageFileInfo.PageFileId;
                    currentFileRevision = pageFileInfo.Revision;
                }

                var newDataHash = Helpers.Crc32(item.Data);

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
                    var pageFile = _db.PageFiles.Single(pf => pf.Id == pageFileId);
                    pageFile.Revision = currentFileRevision;
                    _db.SaveChanges();

                    // Insert new file revision
                    var newRevision = new PageFileRevisionEntityDB
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

                    _db.PageFileRevisions.Add(newRevision);
                    _db.SaveChanges();

                    // Remove previous attachment if exists
                    if (currentlyAttachedFile != null)
                    {
                        var prevAttachment = _db.PageRevisionAttachments
                            .SingleOrDefault(a => a.PageId == item.PageId
                                && a.PageFileId == pageFileId
                                && a.FileRevision == currentlyAttachedFile.Revision
                                && a.PageRevision == currentPageRevision);

                        if (prevAttachment != null)
                        {
                            _db.PageRevisionAttachments.Remove(prevAttachment);
                        }
                    }

                    // Associate file revision with page revision
                    var newAttachment = new PageRevisionAttachmentEntityDB
                    {
                        PageId = item.PageId,
                        PageFileId = pageFileId,
                        FileRevision = currentFileRevision,
                        PageRevision = currentPageRevision
                    };

                    _db.PageRevisionAttachments.Add(newAttachment);
                    _db.SaveChanges();
                }

                transaction.Commit();

                WikiCache.ClearCategory(WikiCache.Category.Page);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}

