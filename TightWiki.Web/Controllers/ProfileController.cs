using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TightWiki.Web.Bff.Interfaces;
using TightWiki.Web.Bff.ViewModels.Profile;
using TightWiki.Web.Filters;

namespace TightWiki.Controllers
{
    [Route("[controller]")]
    public class ProfileController(IProfileBffService profileBff) : Controller
    {
        [AllowAnonymous]
        [HttpGet]
        [HttpGet("{userAccountName}/Avatar")]
        public IActionResult Avatar(GetAvatarRequest request)
            => profileBff.GetAvatar(request);

        [AllowAnonymous]
        [ProducesView]
        [HttpGet("{userAccountName}/Public")]
        public PublicViewModel Public(string userAccountName)
            => profileBff.GetPublicViewModel(userAccountName);

        [Authorize]
        [ProducesView]
        [HttpGet]
        [HttpGet("My")]
        public AccountProfileViewModel My()
            => profileBff.GetMyProfileViewModel();

        [Authorize]
        [HttpPost("My")]
        public IActionResult My(SaveMyProfileRequest request)
            => profileBff.SaveMyProfile(request);

        [Authorize]
        [HttpPost("Delete")]
        public IActionResult DeleteAccount(DeleteMyAccountRequest request)
            => profileBff.DeleteMyAccount(request);

        [Authorize]
        [ProducesView]
        [HttpGet("Deleted")]
        public DeletedAccountViewModel Deleted()
            => new();
    }
}
