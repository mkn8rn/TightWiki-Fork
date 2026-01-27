// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using BLL.Services.Exception;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;
 
using TightWiki.Contracts;

namespace TightWiki.Areas.Identity.Pages.Account
{
    public class LoginWithRecoveryCodeInputModel
    {
        [BindProperty]
        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(TightWiki.Localisation.Resources.ValTexts))]
        [DataType(DataType.Text, ErrorMessageResourceName = "DataTypeAttribute_EmptyDataTypeString", ErrorMessageResourceType = typeof(TightWiki.Localisation.Resources.ValTexts))]
        [Display(Name = "Recovery Code")]
        public string RecoveryCode { get; set; }
    }

    public class LoginWithRecoveryCodeModel : PageModelBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<LoginWithRecoveryCodeModel> _logger;
        private readonly IExceptionService _exceptionService;

        public LoginWithRecoveryCodeModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<LoginWithRecoveryCodeModel> logger,
            IExceptionService exceptionService)
            : base(signInManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _exceptionService = exceptionService;
        }

        [BindProperty]
        public LoginWithRecoveryCodeInputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            try
            {
                ReturnUrl = WebUtility.UrlDecode(returnUrl ?? $"{GlobalConfiguration.BasePath}/");

                // Ensure the user has gone through the username & password screen first
                var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
                if (user == null)
                {
                    throw new InvalidOperationException($"Unable to load two-factor authentication user.");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {Message}", ex.Message);
                _exceptionService.LogException(ex);
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            try
            {
                ReturnUrl = WebUtility.UrlDecode(returnUrl ?? $"{GlobalConfiguration.BasePath}/");

                if (!ModelState.IsValid)
                {
                    return Page();
                }

                var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
                if (user == null)
                {
                    throw new InvalidOperationException($"Unable to load two-factor authentication user.");
                }

                var recoveryCode = Input.RecoveryCode.Replace(" ", string.Empty);

                var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

                var userId = await _userManager.GetUserIdAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User with ID '{UserId}' logged in with a recovery code.", user.Id);
                    return Redirect(ReturnUrl);
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/Lockout");
                }
                else
                {
                    _logger.LogWarning("Invalid recovery code entered for user with ID '{UserId}' ", user.Id);
                    ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
                    return Page();
                }
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

