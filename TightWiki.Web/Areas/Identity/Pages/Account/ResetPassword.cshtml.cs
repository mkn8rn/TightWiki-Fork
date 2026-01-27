// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using BLL.Services.Exception;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;
 
using TightWiki.Contracts;

namespace TightWiki.Areas.Identity.Pages.Account
{
    public class ResetPasswordInputModel
    {
        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(TightWiki.Localisation.Resources.ValTexts))]
        [EmailAddress(ErrorMessageResourceName = "EmailAddressAttribute_Invalid", ErrorMessageResourceType = typeof(TightWiki.Localisation.Resources.ValTexts))]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(TightWiki.Localisation.Resources.ValTexts))]
        [StringLength(100, MinimumLength = 6, ErrorMessageResourceName = "StringLengthAttribute_ValidationErrorIncludingMinimum", ErrorMessageResourceType = typeof(TightWiki.Localisation.Resources.ValTexts))]
        [DataType(DataType.Password, ErrorMessageResourceName = "DataTypeAttribute_EmptyDataTypeString", ErrorMessageResourceType = typeof(TightWiki.Localisation.Resources.ValTexts))]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password, ErrorMessageResourceName = "DataTypeAttribute_EmptyDataTypeString", ErrorMessageResourceType = typeof(TightWiki.Localisation.Resources.ValTexts))]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessageResourceName = "CompareAttribute_MustMatch", ErrorMessageResourceType = typeof(TightWiki.Localisation.Resources.ValTexts))]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(TightWiki.Localisation.Resources.ValTexts))]
        [Display(Name = "Code")]
        public string Code { get; set; }
    }

    public class ResetPasswordModel : PageModelBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ResetPasswordModel> _logger;
        private readonly IExceptionService _exceptionService;

        public ResetPasswordModel(
            ILogger<ResetPasswordModel> logger,
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IExceptionService exceptionService)
            : base(signInManager)
        {
            _logger = logger;
            _userManager = userManager;
            _exceptionService = exceptionService;
        }

        [BindProperty]
        public ResetPasswordInputModel Input { get; set; }

        public IActionResult OnGet(string encodedCode = null)
        {
            try
            {
                if (encodedCode == null)
                {
                    return BadRequest("A code must be supplied for password reset.");
                }
                else
                {
                    Input = new ResetPasswordInputModel
                    {
                        Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedCode))
                    };
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

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Page();
                }

                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/ResetPasswordConfirmation");
                }

                var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
                if (result.Succeeded)
                {
                    return Redirect($"{GlobalConfiguration.BasePath}/Identity/Account/ResetPasswordConfirmation");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
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

