using BLL.Services.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NTDLS.Helpers;
using System.Security.Claims;
using TightWiki.Contracts;
using TightWiki.Web.Bff.Extensions;
using TightWiki.Web.Bff.Interfaces;
using TightWiki.Web.Bff.ViewModels.Account;

namespace TightWiki.Web.Bff.Services
{
    public class AccountService(
        IUsersService usersService,
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IStringLocalizer<AccountService> localizer)
        : IAccountBffService
    {
        public IActionResult ExternalLogin(ExternalLoginRequest request)
        {
            var returnUrl = System.Net.WebUtility.UrlDecode(request.ReturnUrl ?? "~/");
            var redirectUrl = $"{GlobalConfiguration.BasePath}/Identity/Account/ExternalLoginCallback?ReturnUrl={Uri.EscapeDataString(returnUrl)}";
            var properties = signInManager.ConfigureExternalAuthenticationProperties(request.Provider, redirectUrl);
            return new ChallengeResult(request.Provider, properties);
        }

        public async Task<IActionResult> ExternalLoginCallback(ExternalLoginCallbackRequest request)
        {
            var returnUrl = System.Net.WebUtility.UrlDecode(request.ReturnUrl ?? "~/");

            if (request.RemoteError != null)
                throw new Exception(localizer.Localize("Error from external provider: {0}", request.RemoteError));

            var info = await signInManager.GetExternalLoginInfoAsync()
                ?? throw new Exception(localizer.Localize("Failed to get information from external provider"));

            var user = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (user != null)
            {
                await signInManager.SignInAsync(user, isPersistent: false);

                var profile = usersService.GetBasicProfileByUserId(Guid.Parse(user.Id));
                if (profile == null)
                {
                    if (GlobalConfiguration.AllowSignup != true)
                        return new RedirectResult($"{GlobalConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");

                    return new RedirectToPageResult($"{GlobalConfiguration.BasePath}/Account/ExternalLoginSupplemental", new { ReturnUrl = returnUrl });
                }

                return new RedirectResult(returnUrl);
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email).EnsureNotNull();
            if (string.IsNullOrEmpty(email))
                throw new Exception(localizer.Localize("The email address was not supplied by the external provider."));

            user = await userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var result = await userManager.AddLoginAsync(user, info);
                if (!result.Succeeded)
                    throw new Exception(string.Join("<br />\r\n", result.Errors.Select(o => o.Description)));

                await signInManager.SignInAsync(user, isPersistent: false);
                return new RedirectResult(returnUrl);
            }

            if (GlobalConfiguration.AllowSignup != true)
                return new RedirectResult($"{GlobalConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");

            return new RedirectToPageResult($"{GlobalConfiguration.BasePath}/Account/ExternalLoginSupplemental", new { ReturnUrl = returnUrl });
        }
    }
}
