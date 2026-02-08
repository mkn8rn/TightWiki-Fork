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
    public class NamespacesController : ControllerBase
    {
        private readonly BackgroundJobService _jobs;

        public NamespacesController(BackgroundJobService jobs) => _jobs = jobs;

        [HttpGet]
        public AcceptedResult GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] string? orderBy = null,
            [FromQuery] string? orderByDirection = null)
            => _jobs.Enqueue(Url, sp =>
            {
                var svc = sp.GetRequiredService<IPageService>();
                var namespaces = svc.GetAllNamespacesPaged(pageNumber, orderBy, orderByDirection);
                return new PaginatedResult<NamespaceDto>
                {
                    PageNumber = pageNumber,
                    PageCount = namespaces.FirstOrDefault()?.PaginationPageCount ?? 0,
                    Items = namespaces.Select(n => new NamespaceDto
                    {
                        Namespace = n.Namespace, CountOfPages = n.CountOfPages
                    }).ToList()
                };
            });

        [HttpGet("{namespaceName}/pages")]
        public AcceptedResult GetPages(string namespaceName,
            [FromQuery] int pageNumber = 1,
            [FromQuery] string? orderBy = null,
            [FromQuery] string? orderByDirection = null)
            => _jobs.Enqueue(Url, sp =>
            {
                var svc = sp.GetRequiredService<IPageService>();
                var pages = svc.GetAllNamespacePagesPaged(pageNumber, namespaceName, orderBy, orderByDirection);
                return new PaginatedResult<PageSummaryDto>
                {
                    PageNumber = pageNumber,
                    PageCount = pages.FirstOrDefault()?.PaginationPageCount ?? 0,
                    Items = pages.Select(p => new PageSummaryDto
                    {
                        Id = p.Id, Name = p.Name, Navigation = p.Navigation,
                        Description = p.Description, ModifiedDate = p.ModifiedDate,
                        ModifiedByUserName = p.ModifiedByUserName
                    }).ToList()
                };
            });
    }
}
