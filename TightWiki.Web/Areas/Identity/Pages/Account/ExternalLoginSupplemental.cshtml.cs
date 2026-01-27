using BLL.Services.Configuration;
using BLL.Services.Exception;
using BLL.Services.Security;
using BLL.Services.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NTDLS.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using TightWiki.Utils;
 
using TightWiki.Contracts;
using TightWiki.Localisation;

namespace TightWiki.Areas.Identity.Pages.Account
{
    public class ExternalLoginSupplementalInputModel
    {
        public List<TimeZoneItem> TimeZones { get; set; } = new();
        public List<CountryItem> Countries { get; set; } = new();
        public List<LanguageItem> Languages { get; set; } = new();


        [Display(Name = "Display Name")]
        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(TightWiki.Localisation.Resources.ValTexts))]
        public string AccountName { get; set; } = string.Empty;

        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; } = string.Empty;

        [Display(Name = "Time-Zone")]
        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(TightWiki.Localisation.Resources.ValTexts))]
        public string TimeZone { get; set; } = string.Empty;

        [Display(Name = "Country")]
        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(TightWiki.Localisation.Resources.ValTexts))]
        public string Country { get; set; } = string.Empty;

        [Display(Name = "Language")]
        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(TightWiki.Localisation.Resources.ValTexts))]
        public string Language { get; set; } = string.Empty;
    }


    public class ExternalLoginSupplementalModel : PageModelBase
    {
        [BindProperty]
        public string? ReturnUrl { get; set; }

        private UserManager<IdentityUser> _userManager;
        private readonly IStringLocalizer<ExternalLoginSupplementalModel> _localizer;
        private readonly ILogger<ExternalLoginSupplementalModel> _logger;
        private readonly IExceptionService _exceptionService;
        private readonly IConfigurationService _configurationService;
        private readonly IUsersService _usersService;
        private readonly ISecurityService _securityService;

        public ExternalLoginSupplementalModel(
            ILogger<ExternalLoginSupplementalModel> logger,
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            IStringLocalizer<ExternalLoginSupplementalModel> localizer,
            IExceptionService exceptionService,
            IConfigurationService configurationService,
            IUsersService usersService,
            ISecurityService securityService)
            : base(signInManager)
        {
            _logger = logger;
            _userManager = userManager;
            _localizer = localizer;
            _exceptionService = exceptionService;
            _configurationService = configurationService;
            _usersService = usersService;
            _securityService = securityService;
        }

        [BindProperty]
        public ExternalLoginSupplementalInputModel Input { get; set; } = new ExternalLoginSupplementalInputModel();

        public IActionResult OnGet()
        {
            try
            {
                ReturnUrl = WebUtility.UrlDecode(ReturnUrl ?? $"{GlobalConfiguration.BasePath}/");


                if (GlobalConfiguration.AllowSignup != true)
                {
                    return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
                }

                PopulateDefaults();
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
                _exceptionService.LogException(ex);
            }
            return Page();
        }

        private void PopulateDefaults()
        {
            Input.TimeZones = TimeZoneItem.GetAll();
            Input.Countries = CountryItem.GetAll();
            Input.Languages = LanguageItem.GetAll();

            var membershipConfig = _configurationService.GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.Membership);

            if (string.IsNullOrEmpty(Input.TimeZone))
                Input.TimeZone = membershipConfig.Value<string>("Default TimeZone").EnsureNotNull();

            if (string.IsNullOrEmpty(Input.Country))
                Input.Country = membershipConfig.Value<string>("Default Country").EnsureNotNull();

            if (string.IsNullOrEmpty(Input.Language))
                Input.Language = membershipConfig.Value<string>("Default Language").EnsureNotNull();
        }



        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                ReturnUrl = WebUtility.UrlDecode(ReturnUrl ?? $"{GlobalConfiguration.BasePath}/");

                if (GlobalConfiguration.AllowSignup != true)
                {
                    return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
                }

                PopulateDefaults();

                if (!ModelState.IsValid)
                {
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(Input.AccountName))
                {
                    ModelState.AddModelError("Input.AccountName", _localizer["Display Name is required."]);
                    return Page();
                }
                else if (_usersService.DoesProfileAccountExist(Input.AccountName))
                {
                    ModelState.AddModelError("Input.AccountName", _localizer["Display Name is already in use."]);
                    return Page();
                }

                var info = await SignInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return NotifyOfError(_localizer["An error occurred retrieving user information from the external provider."]);
                }

                var email = info.Principal.FindFirstValue(ClaimTypes.Email).EnsureNotNull();
                if (string.IsNullOrEmpty(email))
                {
                    return NotifyOfError(_localizer["The email address was not supplied by the external provider."]);
                }

                var user = new IdentityUser { UserName = email, Email = email };
                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    return NotifyOfError(_localizer["An error occurred while creating the user."]);
                }

                result = await _userManager.AddLoginAsync(user, info);
                if (!result.Succeeded)
                {
                    return NotifyOfError(_localizer["An error occurred while adding the login."]);
                }

                var membershipConfig = _configurationService.GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.Membership);
                _usersService.CreateProfile(Guid.Parse(user.Id), Input.AccountName);
                _usersService.AddRoleMemberByName(Guid.Parse(user.Id), membershipConfig.Value<string>("Default Signup Role").EnsureNotNull());

                var claimsToAdd = new List<Claim>
            {
                new ("timezone", Input.TimeZone),
                new (ClaimTypes.Country, Input.Country),
                new ("language", Input.Language),
                new ("firstname", Input.FirstName ?? ""),
                new ("lastname", Input.LastName ?? ""),
            };

                _securityService.UpsertUserClaims(_userManager, user, claimsToAdd);

                await SignInManager.SignInAsync(user, isPersistent: false);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
                _exceptionService.LogException(ex);
            }

            if (string.IsNullOrEmpty(ReturnUrl))
            {
                return Redirect($"{GlobalConfiguration.BasePath}/");
            }
            return Redirect(ReturnUrl);
        }
    }
}

