// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using BLL.Services.Configuration;
using BLL.Services.Email;
using BLL.Services.Exception;
using BLL.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using TightWiki.Utils;
 
using TightWiki.Contracts;

namespace TightWiki.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResendEmailConfirmationModel : PageModelBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailSender;
        private readonly IStringLocalizer<ResendEmailConfirmationModel> _localizer;
        private readonly ILogger<ResendEmailConfirmationModel> _logger;
        private readonly IExceptionService _exceptionService;
        private readonly IConfigurationService _configurationService;
        private readonly IUsersService _usersService;

        public ResendEmailConfirmationModel(
            ILogger<ResendEmailConfirmationModel> logger,
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IEmailService emailSender,
            IStringLocalizer<ResendEmailConfirmationModel> localizer,
            IExceptionService exceptionService,
            IConfigurationService configurationService,
            IUsersService usersService)
            : base(signInManager)
        {
            _logger = logger;
            _userManager = userManager;
            _emailSender = emailSender;
            _localizer = localizer;
            _exceptionService = exceptionService;
            _configurationService = configurationService;
            _usersService = usersService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(TightWiki.Localisation.Resources.ValTexts))]
            [EmailAddress(ErrorMessageResourceName = "EmailAddressAttribute_Invalid", ErrorMessageResourceType = typeof(TightWiki.Localisation.Resources.ValTexts))]
            public string Email { get; set; }
        }

        public IActionResult OnGet()
        {
            try
            {
                if (GlobalConfiguration.AllowSignup != true)
                {
                    return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
                _exceptionService.LogException(ex);
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (GlobalConfiguration.AllowSignup != true)
                {
                    return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/RegistrationIsNotAllowed");
                }
                if (!ModelState.IsValid)
                {
                    return Page();
                }

                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, _localizer["Verification email sent. Please check your email."]);
                    return Page();
                }

                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, code = encodedCode },
                    protocol: Request.Scheme);

                var emailTemplate = new StringBuilder(_configurationService.GetConfigurationValue<string>(Constants.ConfigurationGroup.Membership, "Template: Account Verification Email"));
                var basicConfig = _configurationService.GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.Basic);
                var siteName = basicConfig.Value<string>("Name");
                var address = basicConfig.Value<string>("Address");
                var profile = _usersService.GetAccountProfileByUserId(Guid.Parse(userId));

                var emailSubject = "Confirm your email";
                emailTemplate.Replace("##SUBJECT##", emailSubject);
                emailTemplate.Replace("##ACCOUNTCOUNTRY##", profile.Country);
                emailTemplate.Replace("##ACCOUNTTIMEZONE##", profile.TimeZone);
                emailTemplate.Replace("##ACCOUNTLANGUAGE##", profile.Language);
                emailTemplate.Replace("##ACCOUNTEMAIL##", profile.EmailAddress);
                emailTemplate.Replace("##ACCOUNTNAME##", profile.AccountName);
                emailTemplate.Replace("##PERSONNAME##", $"{profile.FirstName} {profile.LastName}");
                emailTemplate.Replace("##CODE##", code);
                emailTemplate.Replace("##USERID##", userId);
                emailTemplate.Replace("##SITENAME##", siteName);
                emailTemplate.Replace("##SITEADDRESS##", address);
                emailTemplate.Replace("##CALLBACKURL##", HtmlEncoder.Default.Encode(callbackUrl));

                await _emailSender.SendEmailAsync(Input.Email, emailSubject, emailTemplate.ToString());

                ModelState.AddModelError(string.Empty, _localizer["Verification email sent. Please check your email."]);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
                _exceptionService.LogException(ex);
            }
            return Page();
        }
    }
}

