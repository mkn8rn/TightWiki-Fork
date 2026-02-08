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
    public class SearchController : ControllerBase
    {
        private readonly BackgroundJobService _jobs;

        public SearchController(BackgroundJobService jobs) => _jobs = jobs;

        [HttpGet]
        public ActionResult Search(
            [FromQuery] string query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] bool? fuzzy = null)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query is required.");

            var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            return _jobs.Enqueue(Url, sp =>
            {
                var svc = sp.GetRequiredService<IPageService>();
                var pages = svc.PageSearchPaged(terms, pageNumber, allowFuzzyMatching: fuzzy);
                return new PaginatedResult<SearchResultDto>
                {
                    PageNumber = pageNumber,
                    PageCount = pages.FirstOrDefault()?.PaginationPageCount ?? 0,
                    Items = pages.Select(p => new SearchResultDto
                    {
                        Id = p.Id, Name = p.Name, Navigation = p.Navigation,
                        Description = p.Description, Score = p.Score, Match = p.Match
                    }).ToList()
                };
            });
        }
    }
}
