using BLL.Services.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TightWiki.API.Models;
using TightWiki.API.Services;

namespace TightWiki.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("fixed")]
    public class PagesController : ControllerBase
    {
        private readonly BackgroundJobService _jobs;

        public PagesController(BackgroundJobService jobs) => _jobs = jobs;

        [HttpGet]
        public AcceptedResult GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] string? orderBy = null,
            [FromQuery] string? orderByDirection = null)
            => _jobs.Enqueue(Url, sp =>
            {
                var svc = sp.GetRequiredService<IPageService>();
                var pages = svc.GetAllPagesPaged(pageNumber, orderBy, orderByDirection);
                return new PaginatedResult<PageSummaryDto>
                {
                    PageNumber = pageNumber,
                    PageCount = pages.FirstOrDefault()?.PaginationPageCount ?? 0,
                    Items = pages.Select(p => new PageSummaryDto
                    {
                        Id = p.Id, Name = p.Name, Navigation = p.Navigation,
                        Description = p.Description, Namespace = p.Namespace,
                        Revision = p.Revision, ModifiedDate = p.ModifiedDate,
                        ModifiedByUserName = p.ModifiedByUserName
                    }).ToList()
                };
            });

        [HttpGet("by-navigation/{*navigation}")]
        public AcceptedResult GetByNavigation(string navigation, [FromQuery] int? revision = null)
            => _jobs.Enqueue(Url, sp =>
            {
                var svc = sp.GetRequiredService<IPageService>();
                var page = svc.GetPageRevisionByNavigation(navigation, revision);
                if (page == null) return null;
                return new PageDetailDto
                {
                    Id = page.Id, Name = page.Name, Navigation = page.Navigation,
                    Description = page.Description, Namespace = page.Namespace,
                    Body = page.Body, Revision = page.Revision,
                    MostCurrentRevision = page.MostCurrentRevision,
                    ChangeSummary = page.ChangeSummary, CreatedDate = page.CreatedDate,
                    ModifiedDate = page.ModifiedDate, ModifiedByUserName = page.ModifiedByUserName
                };
            });

        [HttpGet("{pageId:int}")]
        public AcceptedResult GetById(int pageId, [FromQuery] int? revision = null)
            => _jobs.Enqueue(Url, sp =>
            {
                var svc = sp.GetRequiredService<IPageService>();
                var page = svc.GetPageRevisionById(pageId, revision);
                if (page == null) return null;
                return new PageDetailDto
                {
                    Id = page.Id, Name = page.Name, Navigation = page.Navigation,
                    Description = page.Description, Namespace = page.Namespace,
                    Body = page.Body, Revision = page.Revision,
                    MostCurrentRevision = page.MostCurrentRevision,
                    ChangeSummary = page.ChangeSummary, CreatedDate = page.CreatedDate,
                    ModifiedDate = page.ModifiedDate, ModifiedByUserName = page.ModifiedByUserName
                };
            });

        [HttpGet("by-navigation/{navigation}/revisions")]
        public AcceptedResult GetRevisions(string navigation,
            [FromQuery] int pageNumber = 1,
            [FromQuery] string? orderBy = null,
            [FromQuery] string? orderByDirection = null)
            => _jobs.Enqueue(Url, sp =>
            {
                var svc = sp.GetRequiredService<IPageService>();
                var revisions = svc.GetPageRevisionsInfoByNavigationPaged(navigation, pageNumber, orderBy, orderByDirection);
                return new PaginatedResult<PageRevisionDto>
                {
                    PageNumber = pageNumber,
                    PageCount = revisions.FirstOrDefault()?.PaginationPageCount ?? 0,
                    Items = revisions.Select(r => new PageRevisionDto
                    {
                        PageId = r.PageId, Revision = r.Revision, HighestRevision = r.HighestRevision,
                        Name = r.Name, Description = r.Description, Navigation = r.Navigation,
                        ChangeSummary = r.ChangeSummary, ModifiedByUserName = r.ModifiedByUserName,
                        ModifiedDate = r.ModifiedDate
                    }).ToList()
                };
            });

        [HttpGet("by-navigation/{navigation}/comments")]
        public AcceptedResult GetComments(string navigation, [FromQuery] int pageNumber = 1)
            => _jobs.Enqueue(Url, sp =>
            {
                var svc = sp.GetRequiredService<IPageService>();
                var comments = svc.GetPageCommentsPaged(navigation, pageNumber);
                return new PaginatedResult<PageCommentDto>
                {
                    PageNumber = pageNumber,
                    PageCount = comments.FirstOrDefault()?.PaginationPageCount ?? 0,
                    Items = comments.Select(c => new PageCommentDto
                    {
                        Id = c.Id, Body = c.Body, UserName = c.UserName, CreatedDate = c.CreatedDate
                    }).ToList()
                };
            });

        [HttpGet("{pageId:int}/tags")]
        public AcceptedResult GetTags(int pageId)
            => _jobs.Enqueue(Url, sp =>
            {
                var svc = sp.GetRequiredService<IPageService>();
                return svc.GetPageTagsById(pageId).Select(t => t.Tag).ToList();
            });

        [HttpGet("{pageId:int}/related")]
        public AcceptedResult GetRelated(int pageId, [FromQuery] int pageNumber = 1)
            => _jobs.Enqueue(Url, sp =>
            {
                var svc = sp.GetRequiredService<IPageService>();
                var related = svc.GetRelatedPagesPaged(pageId, pageNumber);
                return new PaginatedResult<PageSummaryDto>
                {
                    PageNumber = pageNumber,
                    PageCount = related.FirstOrDefault()?.PaginationPageCount ?? 0,
                    Items = related.Select(r => new PageSummaryDto
                    {
                        Id = r.Id, Name = r.Name, Navigation = r.Navigation
                    }).ToList()
                };
            });

        [HttpGet("recent")]
        public AcceptedResult GetRecent([FromQuery] int count = 10)
        {
            if (count is < 1 or > 100) count = 10;
            return _jobs.Enqueue(Url, sp =>
            {
                var svc = sp.GetRequiredService<IPageService>();
                return svc.GetTopRecentlyModifiedPagesInfo(count).Select(p => new PageSummaryDto
                {
                    Id = p.Id, Name = p.Name, Navigation = p.Navigation,
                    Description = p.Description, ModifiedDate = p.ModifiedDate
                }).ToList();
            });
        }

        [HttpGet("missing")]
        public AcceptedResult GetMissing([FromQuery] int pageNumber = 1)
            => _jobs.Enqueue(Url, sp =>
            {
                var svc = sp.GetRequiredService<IPageService>();
                var missing = svc.GetMissingPagesPaged(pageNumber);
                return new PaginatedResult<object>
                {
                    PageNumber = pageNumber,
                    PageCount = missing.FirstOrDefault()?.PaginationPageCount ?? 0,
                    Items = missing.Select(m => (object)new
                    {
                        m.SourcePageId, m.SourcePageName, m.SourcePageNavigation,
                        m.TargetPageName, m.TargetPageNavigation
                    }).ToList()
                };
            });
    }
}
