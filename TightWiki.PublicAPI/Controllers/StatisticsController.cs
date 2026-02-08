using BLL.Services.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TightWiki.API.Models;
using TightWiki.API.Services;

namespace TightWiki.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("fixed")]
    public class StatisticsController : ControllerBase
    {
        private readonly BackgroundJobService _jobs;

        public StatisticsController(BackgroundJobService jobs) => _jobs = jobs;

        [HttpGet]
        public AcceptedResult Get()
            => _jobs.Enqueue(Url, sp =>
            {
                var svc = sp.GetRequiredService<IConfigurationService>();
                var stats = svc.GetDatabaseStatistics();
                return new StatisticsDto
                {
                    Pages = stats.Pages, PageRevisions = stats.PageRevisions,
                    PageAttachments = stats.PageAttachments, PageTags = stats.PageTags,
                    Users = stats.Users, Namespaces = stats.Namespaces
                };
            });
    }
}
