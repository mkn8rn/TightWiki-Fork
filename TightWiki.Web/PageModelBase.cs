using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TightWiki.ViewHelpers;

namespace TightWiki
{
    public class PageModelBase : PageModel
    {
        public SignInManager<IdentityUser> SignInManager { get; private set; }

        public string SuccessMessage { get; set; } = string.Empty;
        public string WarningMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public PageModelBase(SignInManager<IdentityUser> signInManager)
        {
            SignInManager = signInManager;
        }

        [NonAction]
        protected string? GetQueryString(string key)
            => Request.Query[key];

        [NonAction]
        protected string GetQueryString(string key, string defaultValue)
            => ((string?)Request.Query[key]) ?? defaultValue;

        [NonAction]
        protected int GetQueryString(string key, int defaultValue)
            => int.Parse(GetQueryString(key, defaultValue.ToString()));

        [NonAction]
        protected string? GetFormString(string key)
            => Request.Form[key];

        [NonAction]
        protected string GetFormString(string key, string defaultValue)
            => ((string?)Request.Form[key]) ?? defaultValue;

        [NonAction]
        protected int GetFormString(string key, int defaultValue)
            => int.Parse(GetFormString(key, defaultValue.ToString()));

        protected RedirectResult NotifyOfSuccess(string message, string redirectUrl)
            => NotifyHelper.NotifyOfSuccess(message, redirectUrl);

        protected RedirectResult NotifyOfWarning(string message, string redirectUrl)
            => NotifyHelper.NotifyOfWarning(message, redirectUrl);

        protected RedirectResult NotifyOfError(string message, string redirectUrl)
            => NotifyHelper.NotifyOfError(message, redirectUrl);

        protected RedirectResult NotifyOfSuccess(string message)
            => NotifyHelper.NotifyOfSuccess(message);

        protected RedirectResult NotifyOfWarning(string message)
            => NotifyHelper.NotifyOfWarning(message);

        protected RedirectResult NotifyOfError(string message)
            => NotifyHelper.NotifyOfError(message);
    }
}

