using BLL.Services.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TightWiki.API.Models;
using TightWiki.API.Services;

namespace TightWiki.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("fixed")]
    public class ProfilesController : ControllerBase
    {
        private readonly BackgroundJobService _jobs;

        public ProfilesController(BackgroundJobService jobs) => _jobs = jobs;

        [HttpGet]
        public AcceptedResult GetAll([FromQuery] int pageNumber = 1, [FromQuery] string? search = null)
            => _jobs.Enqueue(Url, sp =>
            {
                var svc = sp.GetRequiredService<IUsersService>();
                var profiles = svc.GetAllPublicProfilesPaged(pageNumber, searchToken: search);
                return new PaginatedResult<ProfileDto>
                {
                    PageNumber = pageNumber,
                    PageCount = profiles.FirstOrDefault()?.PaginationPageCount ?? 0,
                    Items = profiles.Select(p => new ProfileDto
                    {
                        AccountName = p.AccountName, Navigation = p.Navigation,
                        FirstName = p.FirstName, LastName = p.LastName,
                        Country = p.Country, Language = p.Language,
                        Biography = p.Biography, CreatedDate = p.CreatedDate,
                        ModifiedDate = p.ModifiedDate
                    }).ToList()
                };
            });

        [HttpGet("{navigation}")]
        public AcceptedResult GetByNavigation(string navigation)
            => _jobs.Enqueue(Url, sp =>
            {
                var svc = sp.GetRequiredService<IUsersService>();
                var profile = svc.GetAccountProfileByNavigation(navigation);
                if (profile == null || string.IsNullOrEmpty(profile.AccountName))
                    return null;
                return new ProfileDto
                {
                    AccountName = profile.AccountName, Navigation = profile.Navigation,
                    FirstName = profile.FirstName, LastName = profile.LastName,
                    Country = profile.Country, Language = profile.Language,
                    Biography = profile.Biography, CreatedDate = profile.CreatedDate,
                    ModifiedDate = profile.ModifiedDate
                };
            });
    }
}
