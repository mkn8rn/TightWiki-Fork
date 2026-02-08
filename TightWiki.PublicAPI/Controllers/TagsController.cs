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
    public class TagsController : ControllerBase
    {
        private readonly BackgroundJobService _jobs;

        public TagsController(BackgroundJobService jobs) => _jobs = jobs;

        [HttpGet("{tag}/associated")]
        public AcceptedResult GetAssociated(string tag)
            => _jobs.Enqueue(Url, sp =>
            {
                var svc = sp.GetRequiredService<IPageService>();
                return svc.GetAssociatedTags(tag).Select(t => new TagDto
                {
                    Tag = t.Tag, PageCount = t.PageCount
                }).ToList();
            });

        [HttpGet("{tag}/pages")]
        public AcceptedResult GetPagesByTag(string tag)
            => _jobs.Enqueue(Url, sp =>
            {
                var svc = sp.GetRequiredService<IPageService>();
                return svc.GetPageInfoByTag(tag).Select(p => new PageSummaryDto
                {
                    Id = p.Id, Name = p.Name, Navigation = p.Navigation, Description = p.Description
                }).ToList();
            });
    }
}
