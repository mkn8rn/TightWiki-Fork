using Microsoft.AspNetCore.Mvc;
using TightWiki.Web.Bff.Interfaces;
using TightWiki.Web.Bff.ViewModels.Account;

namespace TightWiki.Controllers
{
    [Area("Identity")]
    [Route("Identity/Account")]
    public class AccountController(IAccountBffService accountBff) : Controller
    {
        [HttpGet("ExternalLogin")]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLoginHttpGet(ExternalLoginRequest request)
            => accountBff.ExternalLogin(request);

        [HttpPost("ExternalLogin")]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLoginHttpPost(ExternalLoginRequest request)
            => accountBff.ExternalLogin(request);

        public async Task<IActionResult> ExternalLoginCallback(ExternalLoginCallbackRequest request)
            => await accountBff.ExternalLoginCallback(request);
    }
}

