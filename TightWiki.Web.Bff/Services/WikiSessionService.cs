using BLL.Services.Configuration;
using BLL.Services.Pages;
using BLL.Services.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using NTDLS.Helpers;
using System.Security.Claims;
using TightWiki.Utils.Caching;
using TightWiki.Contracts;
using TightWiki.Contracts.DataModels;
using TightWiki.Contracts.Exceptions;
using TightWiki.Contracts.Interfaces;
using TightWiki.Static;
using TightWiki.Utils;
using TightWiki.Web.Bff.Extensions;
using static TightWiki.Contracts.Constants;

namespace TightWiki.Web.Bff.Services
{
    /// <summary>
    /// Scoped service that provides per-request wiki session state.
    /// Self-initializes from the current HttpContext user on first access.
    /// </summary>
    public sealed class WikiSessionService : ISessionState
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IUsersService _usersService;
        private readonly IConfigurationService _configurationService;
        private readonly IPageService _pageService;

        private readonly string _denyString = WikiPermissionDisposition.Deny.ToString();
        private readonly string _allowString = WikiPermissionDisposition.Allow.ToString();

        private bool _initialized;

        public WikiSessionService(
            IHttpContextAccessor httpContextAccessor,
            SignInManager<IdentityUser> signInManager,
            IUsersService usersService,
            IConfigurationService configurationService,
            IPageService pageService)
        {
            _httpContextAccessor = httpContextAccessor;
            _signInManager = signInManager;
            _usersService = usersService;
            _configurationService = configurationService;
            _pageService = pageService;
        }

        #region Identity

        public bool IsAuthenticated { get; private set; }
        public bool IsAdministrator { get; private set; }
        public IAccountProfile? Profile { get; set; }
        public Theme UserTheme { get; set; } = new();
        public List<ApparentPermission> Permissions { get; private set; } = new();

        #endregion

        #region Current page context

        public IPage Page { get; set; } = new Page() { Name = GlobalConfiguration.Name };
        public string? PageTitle { get; set; }
        public bool ShouldCreatePage { get; set; }
        public string PageNavigation { get; set; } = string.Empty;
        public string PageNavigationEscaped { get; set; } = string.Empty;
        public string PageTags { get; set; } = string.Empty;
        public ProcessingInstructionCollection PageInstructions { get; set; } = new();
        public IQueryCollection? QueryString { get; set; }

        #endregion

        /// <summary>
        /// Ensures identity data has been loaded from the current HttpContext.
        /// Called automatically by permission/localization methods, but can be
        /// called explicitly for eager initialization.
        /// </summary>
        public void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            var httpContext = _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("WikiSessionService requires an active HttpContext.");

            QueryString = httpContext.Request.Query;

            // Extract page navigation from route data
            if (httpContext.GetRouteValue("givenCanonical") is string givenCanonical)
            {
                PageNavigation = givenCanonical;
            }
            else
            {
                PageNavigation = "Home";
            }
            PageNavigationEscaped = Uri.EscapeDataString(PageNavigation);

            // Hydrate security context
            UserTheme = GlobalConfiguration.SystemTheme;
            var user = httpContext.User;

            if (_signInManager.IsSignedIn(user))
            {
                try
                {
                    if (user.Identity?.IsAuthenticated == true)
                    {
                        var userId = Guid.Parse((user.Claims.First(x => x.Type == ClaimTypes.NameIdentifier)?.Value).EnsureNotNull());

                        if (_usersService.TryGetBasicProfileByUserId(userId, out var profile))
                        {
                            Profile = profile;
                            IsAdministrator = _usersService.IsUserMemberOfAdministrators(userId);
                            Permissions = _usersService.GetApparentAccountPermissions(userId).ToList();
                            UserTheme = _configurationService.GetAllThemes().SingleOrDefault(o => o.Name == Profile.Theme) ?? GlobalConfiguration.SystemTheme;
                            IsAuthenticated = true;
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    httpContext.SignOutAsync();
                    if (user.Identity != null)
                    {
                        httpContext.SignOutAsync(user.Identity.AuthenticationType);
                    }
                    System.Diagnostics.Debug.WriteLine($"WikiSessionService hydration error: {ex.Message}");
                }
            }

            Permissions = _usersService.GetApparentRolePermissions(WikiRoles.Anonymous.ToString()).ToList();
        }

        #region Page context

        public void SetPageId(int? pageId, int? revision = null)
        {
            EnsureInitialized();

            Page = new Page();
            PageInstructions = new();
            PageTags = string.Empty;

            if (pageId != null)
            {
                Page = _pageService.GetLimitedPageInfoByIdAndRevision((int)pageId, revision)
                    ?? throw new Exception("Page not found");

                PageInstructions = _pageService.GetPageProcessingInstructionsByPageId(Page.Id);

                if (GlobalConfiguration.IncludeWikiTagsInMeta)
                {
                    PageTags = string.Join(",", _pageService.GetPageTagsById(Page.Id)?.Select(o => o.Tag) ?? []);
                }
            }
        }

        #endregion

        #region Permissions

        public bool HoldsPermission(WikiPermission[] permissions)
            => HoldsPermission(Page.Navigation, permissions);

        public bool HoldsPermission(WikiPermission permission)
            => HoldsPermission(Page.Navigation, permission);

        public bool HoldsPermission(string? givenCanonical, WikiPermission permission)
            => HoldsPermission(givenCanonical, [permission]);

        public bool HoldsPermission(string? givenCanonical, WikiPermission[] permissions)
        {
            EnsureInitialized();

            if (IsAdministrator) return true;

            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Security,
                [givenCanonical, Profile?.UserId, string.Join("|", permissions).ToLowerInvariant()]);

            return WikiCache.AddOrGet(cacheKey, () =>
            {
                Page? page = null;

                if (givenCanonical != null)
                {
                    var navigation = new NamespaceNavigation(givenCanonical);
                    page = _pageService.GetPageInfoByNavigation(navigation.Canonical);
                }

                foreach (var permission in permissions)
                {
                    if (EvaluatePermission(permission, permission == WikiPermission.Create ? null : page) == true)
                        return true;
                }
                return false;
            }, WikiCache.DefaultCacheSeconds);
        }

        private bool? EvaluatePermission(WikiPermission permission, Page? page)
        {
            string permissionString = permission.ToString();

            if (page != null)
            {
                var pageIdString = page.Id.ToString();

                if (Permissions.Any(o => o.PageId == pageIdString
                    && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                    && o.PermissionDisposition.Equals(_denyString, StringComparison.InvariantCultureIgnoreCase)))
                    return false;

                if (Permissions.Any(o => o.PageId == pageIdString
                    && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                    && o.PermissionDisposition.Equals(_allowString, StringComparison.InvariantCultureIgnoreCase)))
                    return true;

                if (Permissions.Any(o => o.Namespace?.Equals(page.Namespace, StringComparison.InvariantCultureIgnoreCase) == true
                    && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                    && o.PermissionDisposition.Equals(_denyString, StringComparison.InvariantCultureIgnoreCase)))
                    return false;

                if (Permissions.Any(o => o.Namespace?.Equals(page.Namespace, StringComparison.InvariantCultureIgnoreCase) == true
                    && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                    && o.PermissionDisposition.Equals(_allowString, StringComparison.InvariantCultureIgnoreCase)))
                    return true;
            }

            if (Permissions.Any(o => o.PageId?.Equals("*", StringComparison.InvariantCultureIgnoreCase) == true
                && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                && o.PermissionDisposition.Equals(_denyString, StringComparison.InvariantCultureIgnoreCase)))
                return false;

            if (Permissions.Any(o => o.PageId?.Equals("*", StringComparison.InvariantCultureIgnoreCase) == true
                && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                && o.PermissionDisposition.Equals(_allowString, StringComparison.InvariantCultureIgnoreCase)))
                return true;

            if (Permissions.Any(o => o.Namespace?.Equals("*", StringComparison.InvariantCultureIgnoreCase) == true
                && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                && o.PermissionDisposition.Equals(_denyString, StringComparison.InvariantCultureIgnoreCase)))
                return false;

            if (Permissions.Any(o => o.Namespace?.Equals("*", StringComparison.InvariantCultureIgnoreCase) == true
                && o.Permission.Equals(permissionString, StringComparison.InvariantCultureIgnoreCase)
                && o.PermissionDisposition.Equals(_allowString, StringComparison.InvariantCultureIgnoreCase)))
                return true;

            return null;
        }

        public void RequireAuthorizedPermission()
        {
            EnsureInitialized();
            if (!IsAuthenticated)
                throw new UnauthorizedException(StaticLocalizer.Localizer["You are not authorized"]);
        }

        public void RequirePermission(string? givenCanonical, WikiPermission[] permissions)
        {
            EnsureInitialized();
            if (!HoldsPermission(givenCanonical, permissions))
                throw new UnauthorizedException(StaticLocalizer.Localizer["You do not have permission to perform the action: {0}"]
                    .Format(string.Join(", ", permissions.Select(o => StaticLocalizer.Localizer[o.ToString()]))));
        }

        public void RequirePermission(string? givenCanonical, WikiPermission permission)
        {
            EnsureInitialized();
            if (!HoldsPermission(givenCanonical, permission))
                throw new UnauthorizedException(StaticLocalizer.Localizer["You do not have permission to perform the action: {0}"]
                    .Format(StaticLocalizer.Localizer[permission.ToString()]));
        }

        public void RequireAdminPermission()
        {
            EnsureInitialized();
            if (!IsAdministrator)
                throw new UnauthorizedException(StaticLocalizer.Localizer["You do not have permission to perform the action: {0}"]
                    .Format(StaticLocalizer.Localizer["Administration"].Value));
        }

        #endregion

        #region Localization

        public DateTime LocalizeDateTime(DateTime datetime)
            => TimeZoneInfo.ConvertTimeFromUtc(datetime, GetPreferredTimeZone());

        public TimeZoneInfo GetPreferredTimeZone()
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(Profile?.TimeZone))
                return TimeZoneInfo.FindSystemTimeZoneById(GlobalConfiguration.DefaultTimeZone);
            return TimeZoneInfo.FindSystemTimeZoneById(Profile.TimeZone);
        }

        #endregion
    }
}
