using BLL.Services.Configuration;
using BLL.Services.Pages;
using BLL.Services.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TightWiki.API.Models;
using TightWiki.API.Services;

namespace TightWiki.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("export")]
    public class ExportController : ControllerBase
    {
        private readonly BackgroundJobService _jobs;

        public ExportController(BackgroundJobService jobs) => _jobs = jobs;

        [HttpGet("full")]
        public AcceptedResult ExportFull()
            => _jobs.Enqueue(Url, sp =>
            {
                var pageSvc = sp.GetRequiredService<IPageService>();
                var usersSvc = sp.GetRequiredService<IUsersService>();
                var configSvc = sp.GetRequiredService<IConfigurationService>();

                var stats = configSvc.GetDatabaseStatistics();
                var allPages = pageSvc.GetAllPages();
                var pageDetails = new List<PageDetailDto>();

                foreach (var page in allPages)
                {
                    var full = pageSvc.GetPageRevisionById(page.Id);
                    if (full == null) continue;
                    pageDetails.Add(new PageDetailDto
                    {
                        Id = full.Id, Name = full.Name, Navigation = full.Navigation,
                        Description = full.Description, Namespace = full.Namespace,
                        Body = full.Body, Revision = full.Revision,
                        MostCurrentRevision = full.MostCurrentRevision,
                        ChangeSummary = full.ChangeSummary, CreatedDate = full.CreatedDate,
                        ModifiedDate = full.ModifiedDate, ModifiedByUserName = full.ModifiedByUserName
                    });
                }

                var namespaces = pageSvc.GetAllNamespaces();
                var profiles = usersSvc.GetAllUsers();

                return new DatabaseExportDto
                {
                    ExportedAtUtc = DateTime.UtcNow,
                    Statistics = new StatisticsDto
                    {
                        Pages = stats.Pages, PageRevisions = stats.PageRevisions,
                        PageAttachments = stats.PageAttachments, PageTags = stats.PageTags,
                        Users = stats.Users, Namespaces = stats.Namespaces
                    },
                    Pages = pageDetails,
                    Namespaces = namespaces.Select(n => new NamespaceDto
                    {
                        Namespace = n, CountOfPages = pageDetails.Count(p => p.Namespace == n)
                    }).ToList(),
                    Profiles = profiles.Select(p => new ProfileDto
                    {
                        AccountName = p.AccountName, Navigation = p.Navigation,
                        FirstName = p.FirstName, LastName = p.LastName,
                        Country = p.Country, Language = p.Language,
                        Biography = p.Biography, CreatedDate = p.CreatedDate,
                        ModifiedDate = p.ModifiedDate
                    }).ToList()
                };
            });
    }
}
