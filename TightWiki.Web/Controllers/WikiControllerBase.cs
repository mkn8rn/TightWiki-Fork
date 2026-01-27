using BLL.Services.Configuration;
using BLL.Services.Pages;
using BLL.Services.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using NTDLS.Helpers;
 
using TightWiki.Contracts;

namespace TightWiki.Controllers
{
    public class WikiControllerBase<T> : Controller
    {
        public SessionState SessionState { get; private set; } = new();

        public readonly SignInManager<IdentityUser> SignInManager;
        public readonly UserManager<IdentityUser> UserManager;
        private readonly IStringLocalizer<T> _localizer;
        private readonly IUsersService _usersService;
        private readonly IConfigurationService _configurationService;
        private readonly IPageService _pageService;

        public WikiControllerBase(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IStringLocalizer<T> localizer,
            IUsersService usersService,
            IConfigurationService configurationService,
            IPageService pageService)
        {
            SignInManager = signInManager;
            UserManager = userManager;
            _localizer = localizer;
            _usersService = usersService;
            _configurationService = configurationService;
            _pageService = pageService;
        }

        // Legacy constructor for controllers that don't need all services yet
        public WikiControllerBase(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IStringLocalizer<T> localizer)
        {
            SignInManager = signInManager;
            UserManager = userManager;
            _localizer = localizer;
            // These will be resolved from HttpContext.RequestServices in OnActionExecuting
            _usersService = null!;
            _configurationService = null!;
            _pageService = null!;
        }

        [NonAction]
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Resolve services from the request's service provider if not injected via constructor
            var usersService = _usersService ?? filterContext.HttpContext.RequestServices.GetRequiredService<IUsersService>();
            var configurationService = _configurationService ?? filterContext.HttpContext.RequestServices.GetRequiredService<IConfigurationService>();
            var pageService = _pageService ?? filterContext.HttpContext.RequestServices.GetRequiredService<IPageService>();

            ViewData["SessionState"] = SessionState.Hydrate(SignInManager, this, usersService, configurationService, pageService);
        }

        [NonAction]
        public override RedirectResult Redirect(string? url)
            => base.Redirect(url.EnsureNotNull());

        [NonAction]
        protected V? GetQueryValue<V>(string key)
        {
            if (Request.Query.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
            {
                return Converters.ConvertToNullable<V>(value);
            }
            return default;
        }

        [NonAction]
        protected V GetQueryValue<V>(string key, V defaultValue)
        {
            if (Request.Query.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
            {
                return Converters.ConvertToNullable<V>(value) ?? defaultValue;
            }
            return defaultValue;
        }

        [NonAction]
        protected string? GetFormValue(string key)
            => Request.Form[key];

        [NonAction]
        protected string GetFormValue(string key, string defaultValue)
            => (string?)Request.Form[key] ?? defaultValue;

        [NonAction]
        protected int GetFormValue(string key, int defaultValue)
            => int.Parse(GetFormValue(key, defaultValue.ToString()));

        [NonAction]
        protected string Localize(string key)
            => _localizer[key].Value;

        [NonAction]
        protected string Localize(string key, params object[] objs)
            => string.Format(_localizer[key].Value, objs);

        /// <summary>
        /// Displays the successMessage unless the errorMessage is present.
        /// </summary>
        protected RedirectResult NotifyOf(string successMessage, string errorMessage, string redirectUrl)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(successMessage)}&NotifyErrorMessage={Uri.EscapeDataString(errorMessage)}&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{redirectUrl}")}&RedirectTimeout=5");

        protected RedirectResult NotifyOfSuccess(string message, string redirectUrl)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{redirectUrl}")}&RedirectTimeout=5");

        protected RedirectResult NotifyOfWarning(string message, string redirectUrl)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyWarningMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{redirectUrl}")}");

        protected RedirectResult NotifyOfError(string message, string redirectUrl)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{redirectUrl}")}");

        protected RedirectResult NotifyOfSuccess(string message)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(message)}");

        protected RedirectResult NotifyOfWarning(string message)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyWarningMessage={Uri.EscapeDataString(message)}");

        protected RedirectResult NotifyOfError(string message)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString(message)}");
    }
}

