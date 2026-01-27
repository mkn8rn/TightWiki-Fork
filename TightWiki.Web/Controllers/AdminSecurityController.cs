using BLL.Services.Configuration;
using BLL.Services.Pages;
using BLL.Services.Security;
using BLL.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NTDLS.Helpers;
using System.Security.Claims;
using TightWiki.Web.Bff.ViewModels.AdminSecurity;
using TightWiki.Web.Bff.ViewModels.Shared;
using TightWiki.Web.Bff.ViewModels.Utility;
using TightWiki.Utils.Caching;
using TightWiki.Contracts;
using TightWiki.Contracts.DataModels;
using TightWiki.Contracts.Requests;
using TightWiki.Helpers;
using TightWiki.Localisation;
using TightWiki.Utils;

namespace TightWiki.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class AdminSecurityController : WikiControllerBase<AdminSecurityController>
    {
        private readonly IConfigurationService _configurationService;
        private readonly IPageService _pageService;
        private readonly ISecurityService _securityService;
        private readonly IUsersService _usersService;

        public AdminSecurityController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IConfigurationService configurationService,
            IPageService pageService,
            ISecurityService securityService,
            IUsersService usersService,
            IStringLocalizer<AdminSecurityController> localizer)
            : base(signInManager, userManager, localizer)
        {
            _configurationService = configurationService;
            _pageService = pageService;
            _securityService = securityService;
            _usersService = usersService;
        }

        #region Roles.

        [Authorize]
        [HttpPost("DeleteRole/{roleId:int}")]
        public ActionResult DeleteRole(ConfirmActionViewModel model, int roleId)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                _usersService.DeleteRole(roleId);
                WikiCache.ClearCategory(WikiCache.Category.Security);
                return NotifyOfSuccess(Localize("The specified role has been deleted."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        /// <summary>
        /// This is called by ajax/jquery and does not redirect when authorization fails.
        /// </summary>
        [Authorize]
        [HttpPost("AddAccountMembership")]
        public IActionResult AddAccountMembership([FromBody] AddAccountMembershipRequest request)
        {
            try
            {
                SessionState.RequireAdminPermission();

                AddAccountMembershipResult? result = null;

                bool alreadyExists = _usersService.IsAccountAMemberOfRole(request.UserId, request.RoleId);
                if (!alreadyExists)
                {
                    result = _usersService.AddAccountMembership(request.UserId, request.RoleId);
                }
                WikiCache.ClearCategory(WikiCache.Category.Security);

                return Ok(new { success = true, alreadyExists = alreadyExists, membership = result, message = (string?)null });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// This is called by ajax/jquery and does not redirect when authorization fails.
        /// </summary>
        [Authorize]
        [HttpPost("AddRoleMember")]
        public IActionResult AddRoleMember([FromBody] AddRoleMemberRequest request)
        {
            try
            {
                SessionState.RequireAdminPermission();

                AddRoleMemberResult? result = null;

                bool alreadyExists = _usersService.IsAccountAMemberOfRole(request.UserId, request.RoleId);
                if (!alreadyExists)
                {
                    result = _usersService.AddRoleMember(request.UserId, request.RoleId);
                }
                WikiCache.ClearCategory(WikiCache.Category.Security);

                return Ok(new { success = true, alreadyExists = alreadyExists, membership = result, message = (string?)null });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// This is called by ajax/jquery and does not redirect when authorization fails.
        /// </summary>
        [Authorize]
        [HttpPost("RemoveRoleMember/{roleId:int}/{userId:Guid}")]
        public IActionResult RemoveRoleMember(int roleId, Guid userId)
        {
            try
            {
                SessionState.RequireAdminPermission();
                _usersService.RemoveRoleMember(roleId, userId);
                WikiCache.ClearCategory(WikiCache.Category.Security);

                return Ok(new { success = true, message = (string?)null });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// This is called by ajax/jquery and does not redirect when authorization fails.
        /// </summary>
        [Authorize]
        [HttpPost("RemoveRolePermission/{id:int}")]
        public IActionResult RemoveRolePermission(int id)
        {
            try
            {
                SessionState.RequireAdminPermission();
                _usersService.RemoveRolePermission(id);
                WikiCache.ClearCategory(WikiCache.Category.Security);

                return Ok(new { success = true, message = (string?)null });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// This is called by ajax/jquery and does not redirect when authorization fails.
        /// </summary>
        [Authorize]
        [HttpPost("AddRolePermission")]
        public IActionResult AddRolePermission([FromBody] AddRolePermissionRequest request)
        {
            try
            {
                SessionState.RequireAdminPermission();

                InsertRolePermissionResult? result = null;

                bool alreadyExists = _usersService.IsRolePermissionDefined(
                    request.RoleId, request.PermissionId, request.PermissionDispositionId, request.Namespace, request.PageId);
                if (!alreadyExists)
                {
                    result = _usersService.InsertRolePermission(
                        request.RoleId, request.PermissionId, request.PermissionDispositionId, request.Namespace, request.PageId);
                }
                WikiCache.ClearCategory(WikiCache.Category.Security);

                return Ok(new { success = true, alreadyExists = alreadyExists, permission = result, message = (string?)null });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("AddRole")]
        public ActionResult AddRole()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Add Role");

            return View(new AddRoleViewModel());
        }

        [Authorize]
        [HttpPost("AddRole")]
        public ActionResult AddRole(AddRoleViewModel model)
        {
            SessionState.RequireAdminPermission();
            SessionState.Page.Name = Localize("Add Role");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", Localize("Role name is required."));
                return View(model);
            }

            if (_usersService.DoesRoleExist(model.Name))
            {
                ModelState.AddModelError("Name", Localize("Role name is already in use."));
                return View(model);
            }

            _usersService.InsertRole(model.Name, model.Description);
            WikiCache.ClearCategory(WikiCache.Category.Security);

            return Redirect($"{GlobalConfiguration.BasePath}/AdminSecurity/Roles");
        }

        [Authorize]
        [HttpGet("Role/{navigation}")]
        public ActionResult Role(string navigation)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Roles");

            navigation = Navigation.Clean(navigation);

            var role = _usersService.GetRoleByName(navigation);

            var model = new RoleViewModel()
            {
                IsBuiltIn = role.IsBuiltIn,
                Id = role.Id,
                Name = role.Name,

                Members = _usersService.GetRoleMembersPaged(role.Id,
                    GetQueryValue("Page_Members", 1), GetQueryValue<string>("OrderBy_Members"), GetQueryValue<string>("OrderByDirection_Members")),

                AssignedPermissions = _usersService.GetRolePermissionsPaged(role.Id,
                    GetQueryValue("Page_Permissions", 1), GetQueryValue<string>("OrderBy_Permission"), GetQueryValue<string>("OrderByDirection_Permissions")),

                PermissionDispositions = _usersService.GetAllPermissionDispositions(),
                Permissions = _usersService.GetAllPermissions()
            };

            model.PaginationPageCount_Members = (model.Members.FirstOrDefault()?.PaginationPageCount ?? 0);
            model.PaginationPageCount_Permissions = (model.AssignedPermissions.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }

        [Authorize]
        [HttpGet("Roles")]
        public ActionResult Roles()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var orderBy = GetQueryValue<string>("OrderBy");
            var orderByDirection = GetQueryValue<string>("OrderByDirection");

            var model = new RolesViewModel()
            {
                Roles = _usersService.GetAllRoles(orderBy, orderByDirection)
            };

            return View(model);
        }

        #endregion

        #region Account Roles.

        /// <summary>
        /// This is called by ajax/jquery and does not redirect when authorization fails.
        /// </summary>
        [Authorize]
        [HttpPost("AddAccountPermission")]
        public IActionResult AddAccountPermission([FromBody] AddAccountPermissionRequest request)
        {
            try
            {
                SessionState.RequireAdminPermission();

                InsertAccountPermissionResult? result = null;

                bool alreadyExists = _usersService.IsAccountPermissionDefined(
                    request.UserId, request.PermissionId, request.PermissionDispositionId, request.Namespace, request.PageId);
                if (!alreadyExists)
                {
                    result = _usersService.InsertAccountPermission(
                        request.UserId, request.PermissionId, request.PermissionDispositionId, request.Namespace, request.PageId);
                }
                WikiCache.ClearCategory(WikiCache.Category.Security);

                return Ok(new { success = true, alreadyExists = alreadyExists, permission = result, message = (string?)null });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// This is called by ajax/jquery and does not redirect when authorization fails.
        /// </summary>
        [Authorize]
        [HttpPost("RemoveAccountPermission/{id:int}")]
        public IActionResult RemoveAccountPermission(int id)
        {
            try
            {
                SessionState.RequireAdminPermission();
                _usersService.RemoveAccountPermission(id);
                WikiCache.ClearCategory(WikiCache.Category.Security);

                return Ok(new { success = true, message = (string?)null });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("AccountRoles/{navigation}")]
        public ActionResult AccountRoles(string navigation)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Roles");

            navigation = Navigation.Clean(navigation);

            var profile = _usersService.GetAccountProfileByNavigation(navigation);

            var model = new AccountRolesViewModel()
            {
                Id = profile.UserId,
                AccountName = profile.AccountName,

                Memberships = _usersService.GetAccountRoleMembershipPaged(profile.UserId,
                    GetQueryValue("Page_Memberships", 1), GetQueryValue<string>("OrderBy_Members"), GetQueryValue<string>("OrderByDirection_Memberships")),

                AssignedPermissions = _usersService.GetAccountPermissionsPaged(profile.UserId,
                    GetQueryValue("Page_Permissions", 1), GetQueryValue<string>("OrderBy_Permissions"), GetQueryValue<string>("OrderByDirection_Permissions")),

                PermissionDispositions = _usersService.GetAllPermissionDispositions(),
                Permissions = _usersService.GetAllPermissions()
            };

            model.PaginationPageCount_Members = (model.Memberships.FirstOrDefault()?.PaginationPageCount ?? 0);
            model.PaginationPageCount_Permissions = (model.AssignedPermissions.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }

        #endregion

        #region Accounts

        [Authorize]
        [HttpGet("Account/{navigation}")]
        public ActionResult Account(string navigation)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var model = new AccountProfileViewModel()
            {
                AccountProfile = AccountProfileAccountViewModel.FromDataModel(
                    _usersService.GetAccountProfileByNavigation(Navigation.Clean(navigation))),
                Credential = new CredentialViewModel(),
                Themes = _configurationService.GetAllThemes(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Roles = _usersService.GetAllRoles()
            };

            model.AccountProfile.CreatedDate = SessionState.LocalizeDateTime(model.AccountProfile.CreatedDate);
            model.AccountProfile.ModifiedDate = SessionState.LocalizeDateTime(model.AccountProfile.ModifiedDate);

            return View(model);
        }

        /// <summary>
        /// Save user profile.
        /// </summary>
        [Authorize]
        [HttpPost("Account/{navigation}")]
        public ActionResult Account(string navigation, TightWiki.Web.Bff.ViewModels.AdminSecurity.AccountProfileViewModel model)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            model.Themes = _configurationService.GetAllThemes();
            model.TimeZones = TimeZoneItem.GetAll();
            model.Countries = CountryItem.GetAll();
            model.Languages = LanguageItem.GetAll();
            model.Roles = _usersService.GetAllRoles();
            model.AccountProfile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName.ToLowerInvariant());

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = UserManager.FindByIdAsync(model.AccountProfile.UserId.ToString()).Result.EnsureNotNull();

            if (model.Credential.Password != CredentialViewModel.NOTSET && model.Credential.Password == model.Credential.ComparePassword)
            {
                try
                {
                    var token = UserManager.GeneratePasswordResetTokenAsync(user).Result.EnsureNotNull();
                    var result = UserManager.ResetPasswordAsync(user, token, model.Credential.Password).Result.EnsureNotNull();
                    if (!result.Succeeded)
                    {
                        throw new Exception(string.Join("<br />\r\n", result.Errors.Select(o => o.Description)));
                    }

                    if (model.AccountProfile.AccountName.Equals(Constants.DEFAULTACCOUNT, StringComparison.InvariantCultureIgnoreCase))
                    {
                        _usersService.SetAdminPasswordIsChanged();
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Credential.Password", ex.Message);
                    return View(model);
                }
            }

            var profile = _usersService.GetAccountProfileByUserId(model.AccountProfile.UserId);
            if (!profile.Navigation.Equals(model.AccountProfile.Navigation, StringComparison.InvariantCultureIgnoreCase))
            {
                if (_usersService.DoesProfileAccountExist(model.AccountProfile.AccountName))
                {
                    ModelState.AddModelError("AccountProfile.AccountName", Localize("Account name is already in use."));
                    return View(model);
                }
            }

            if (!profile.EmailAddress.Equals(model.AccountProfile.EmailAddress, StringComparison.InvariantCultureIgnoreCase))
            {
                if (_usersService.DoesEmailAddressExist(model.AccountProfile.EmailAddress))
                {
                    ModelState.AddModelError("AccountProfile.EmailAddress", Localize("Email address is already in use."));
                    return View(model);
                }
            }

            var file = Request.Form.Files["Avatar"];
            if (file != null && file.Length > 0)
            {
                if (GlobalConfiguration.AllowableImageTypes.Contains(file.ContentType.ToLowerInvariant()) == false)
                {
                    model.ErrorMessage += Localize("Could not save the attached image, type not allowed.") + "\r\n";
                }
                else if (file.Length > GlobalConfiguration.MaxAvatarFileSize)
                {
                    model.ErrorMessage += Localize("Could not save the attached image, too large.") + "\r\n";
                }
                else
                {
                    try
                    {
                        var imageBytes = WebUtility.ConvertHttpFileToBytes(file);
                        var image = WebUtility.CropImageToCenteredSquare(new MemoryStream(imageBytes));
                        _usersService.UpdateProfileAvatar(profile.UserId, image, "image/webp");
                    }
                    catch
                    {
                        model.ErrorMessage += Localize("Could not save the attached image.") + "\r\n";
                    }
                }
            }

            profile.AccountName = model.AccountProfile.AccountName;
            profile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName);
            profile.Biography = model.AccountProfile.Biography;
            profile.ModifiedDate = DateTime.UtcNow;
            _usersService.UpdateProfile(profile);

            var claims = new List<Claim>
                    {
                        new ("timezone", model.AccountProfile.TimeZone),
                        new (ClaimTypes.Country, model.AccountProfile.Country),
                        new ("language", model.AccountProfile.Language),
                        new ("firstname", model.AccountProfile.FirstName ?? ""),
                        new ("lastname", model.AccountProfile.LastName ?? ""),
                        new ("theme", model.AccountProfile.Theme ?? ""),
                    };
            _securityService.UpsertUserClaims(UserManager, user, claims);

            //If we are changing the currently logged in user, then make sure we take some extra actions so we can see the changes immediately.
            if (SessionState.Profile?.UserId == model.AccountProfile.UserId)
            {
                SignInManager.RefreshSignInAsync(user);

                WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.Navigation]));
                WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.UserId]));

                //This is not 100% necessary, I just want to prevent the user from needing to refresh to view the new theme.
                SessionState.UserTheme = _configurationService.GetAllThemes().SingleOrDefault(o => o.Name == model.AccountProfile.Theme) ?? GlobalConfiguration.SystemTheme;
            }

            //Allow the administrator to confirm/unconfirm the email address.
            bool emailConfirmChanged = profile.EmailConfirmed != model.AccountProfile.EmailConfirmed;
            if (emailConfirmChanged)
            {
                user.EmailConfirmed = model.AccountProfile.EmailConfirmed;
                var updateResult = UserManager.UpdateAsync(user).Result;
                if (!updateResult.Succeeded)
                {
                    throw new Exception(string.Join("<br />\r\n", updateResult.Errors.Select(o => o.Description)));
                }
            }

            if (!profile.EmailAddress.Equals(model.AccountProfile.EmailAddress, StringComparison.InvariantCultureIgnoreCase))
            {
                bool wasEmailAlreadyConfirmed = user.EmailConfirmed;

                var setEmailResult = UserManager.SetEmailAsync(user, model.AccountProfile.EmailAddress).Result;
                if (!setEmailResult.Succeeded)
                {
                    throw new Exception(string.Join("<br />\r\n", setEmailResult.Errors.Select(o => o.Description)));
                }

                var setUserNameResult = UserManager.SetUserNameAsync(user, model.AccountProfile.EmailAddress).Result;
                if (!setUserNameResult.Succeeded)
                {
                    throw new Exception(string.Join("<br />\r\n", setUserNameResult.Errors.Select(o => o.Description)));
                }

                //If the email address was already confirmed, just keep the status. Afterall, this is an admin making the change.
                if (wasEmailAlreadyConfirmed && emailConfirmChanged == false)
                {
                    user.EmailConfirmed = true;
                    var updateResult = UserManager.UpdateAsync(user).Result;
                    if (!updateResult.Succeeded)
                    {
                        throw new Exception(string.Join("<br />\r\n", updateResult.Errors.Select(o => o.Description)));
                    }
                }
            }

            model.SuccessMessage = Localize("Your profile has been saved successfully!");
            WikiCache.ClearCategory(WikiCache.Category.Security);

            return View(model);
        }

        [Authorize]
        [HttpGet("AddAccount")]
        public ActionResult AddAccount()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var membershipConfig = _configurationService.GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.Membership);
            var defaultSignupRole = membershipConfig.Value<string>("Default Signup Role").EnsureNotNull();
            var customizationConfig = _configurationService.GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.Customization);

            var model = new AccountProfileViewModel()
            {
                AccountProfile = new AccountProfileAccountViewModel
                {
                    AccountName = string.Empty,
                    Country = customizationConfig.Value<string>("Default Country", string.Empty),
                    TimeZone = customizationConfig.Value<string>("Default TimeZone", string.Empty),
                    Language = customizationConfig.Value<string>("Default Language", string.Empty)
                },
                DefaultRole = defaultSignupRole,
                Themes = _configurationService.GetAllThemes(),
                Credential = new CredentialViewModel(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Roles = _usersService.GetAllRoles()
            };

            return View(model);
        }

        /// <summary>
        /// Create a new user profile.
        /// </summary>
        [Authorize]
        [HttpPost("AddAccount")]
        public ActionResult AddAccount(TightWiki.Web.Bff.ViewModels.AdminSecurity.AccountProfileViewModel model)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            model.Themes = _configurationService.GetAllThemes();
            model.TimeZones = TimeZoneItem.GetAll();
            model.Countries = CountryItem.GetAll();
            model.Languages = LanguageItem.GetAll();
            model.Roles = _usersService.GetAllRoles();
            model.AccountProfile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName?.ToLowerInvariant());

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.AccountProfile.AccountName))
            {
                ModelState.AddModelError("AccountProfile.AccountName", Localize("Account name is required."));
                return View(model);
            }

            if (_usersService.DoesProfileAccountExist(model.AccountProfile.AccountName))
            {
                ModelState.AddModelError("AccountProfile.AccountName", Localize("Account name is already in use."));
                return View(model);
            }

            if (_usersService.DoesEmailAddressExist(model.AccountProfile.EmailAddress))
            {
                ModelState.AddModelError("AccountProfile.EmailAddress", Localize("Email address is already in use."));
                return View(model);
            }

            Guid? userId;

            try
            {
                //Define the new user:
                var identityUser = new IdentityUser(model.AccountProfile.EmailAddress)
                {
                    Email = model.AccountProfile.EmailAddress,
                    EmailConfirmed = true
                };

                //Create the new user:
                var creationResult = UserManager.CreateAsync(identityUser, model.Credential.Password).Result;
                if (!creationResult.Succeeded)
                {
                    model.ErrorMessage = string.Join("<br />\r\n", creationResult.Errors.Select(o => o.Description));
                    return View(model);
                }
                identityUser = UserManager.FindByEmailAsync(model.AccountProfile.EmailAddress).Result.EnsureNotNull();

                userId = Guid.Parse(identityUser.Id);

                //Insert the claims.
                var claims = new List<Claim>
                    {
                        new ("timezone", model.AccountProfile.TimeZone),
                        new (ClaimTypes.Country, model.AccountProfile.Country),
                        new ("language", model.AccountProfile.Language),
                        new ("firstname", model.AccountProfile.FirstName ?? ""),
                        new ("lastname", model.AccountProfile.LastName ?? ""),
                        new ("theme", model.AccountProfile.Theme ?? ""),
                    };
                _securityService.UpsertUserClaims(UserManager, identityUser, claims);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.Message);
            }

            _usersService.CreateProfile(userId.Value, model.AccountProfile.AccountName);
            _usersService.AddRoleMemberByName(userId.Value, model.DefaultRole);

            var profile = _usersService.GetAccountProfileByUserId(userId.Value);

            profile.AccountName = model.AccountProfile.AccountName;
            profile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName);
            profile.Biography = model.AccountProfile.Biography;
            profile.ModifiedDate = DateTime.UtcNow;
            _usersService.UpdateProfile(profile);

            var file = Request.Form.Files["Avatar"];
            if (file != null && file.Length > 0)
            {
                if (GlobalConfiguration.AllowableImageTypes.Contains(file.ContentType.ToLowerInvariant()) == false)
                {
                    model.ErrorMessage += Localize("Could not save the attached image, type not allowed.") + "\r\n";
                }
                else if (file.Length > GlobalConfiguration.MaxAvatarFileSize)
                {
                    model.ErrorMessage += Localize("Could not save the attached image, too large.") + "\r\n";
                }
                else
                {
                    try
                    {
                        var imageBytes = WebUtility.ConvertHttpFileToBytes(file);
                        var image = WebUtility.CropImageToCenteredSquare(new MemoryStream(imageBytes));
                        _usersService.UpdateProfileAvatar(profile.UserId, image, "image/webp");
                    }
                    catch
                    {
                        model.ErrorMessage += Localize("Could not save the attached image.");
                    }
                }
            }
            WikiCache.ClearCategory(WikiCache.Category.Security);

            return NotifyOf(Localize("The account has been created."), model.ErrorMessage, $"/AdminSecurity/Account/{profile.Navigation}");
        }

        [Authorize]
        [HttpGet("Accounts")]
        public ActionResult Accounts()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue<string>("OrderBy");
            var orderByDirection = GetQueryValue<string>("OrderByDirection");
            var searchString = GetQueryValue("SearchString", string.Empty);

            var model = new AccountsViewModel()
            {
                Users = _usersService.GetAllUsersPaged(pageNumber, orderBy, orderByDirection, searchString),
                SearchString = searchString
            };

            model.PaginationPageCount = (model.Users.FirstOrDefault()?.PaginationPageCount ?? 0);

            if (model.Users != null && model.Users.Count > 0)
            {
                model.Users.ForEach(o =>
                {
                    o.CreatedDate = SessionState.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = SessionState.LocalizeDateTime(o.ModifiedDate);
                });
            }

            return View(model);
        }

        [Authorize]
        [HttpPost("DeleteAccount/{navigation}")]
        public ActionResult DeleteAccount(ConfirmActionViewModel model, string navigation)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                var profile = _usersService.GetAccountProfileByNavigation(navigation);

                var user = UserManager.FindByIdAsync(profile.UserId.ToString()).Result;
                if (user == null)
                {
                    return NotFound(Localize("User not found."));
                }

                var result = UserManager.DeleteAsync(user).Result;
                if (!result.Succeeded)
                {
                    throw new Exception(string.Join("<br />\r\n", result.Errors.Select(o => o.Description)));
                }

                _usersService.AnonymizeProfile(profile.UserId);
                WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.Navigation]));

                if (profile.UserId == SessionState.Profile?.UserId)
                {
                    //We're deleting our own account. Oh boy...
                    SignInManager.SignOutAsync();

                    WikiCache.ClearCategory(WikiCache.Category.Security);
                    return NotifyOfSuccess(Localize("Your account has been deleted."), $"/Profile/Deleted");
                }
                WikiCache.ClearCategory(WikiCache.Category.Security);

                return NotifyOfSuccess(Localize("The account has been deleted."), $"/AdminSecurity/Accounts");
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        #endregion

        #region AutoComplete.

        [Authorize]
        [HttpGet("AutoCompleteRole")]
        public ActionResult AutoCompleteRole([FromQuery] string? q = null)
        {
            var roles = _usersService.AutoCompleteRole(q).ToList();

            return Json(roles.Select(o => new
            {
                text = o.Name,
                id = o.Id.ToString()
            }));
        }

        [Authorize]
        [HttpGet("AutoCompleteAccount")]
        public ActionResult AutoCompleteAccount([FromQuery] string? q = null)
        {
            var accounts = _usersService.AutoCompleteAccount(q).ToList();

            return Json(accounts.Select(o => new
            {
                text = string.IsNullOrWhiteSpace(o.EmailAddress) ? o.AccountName : $"{o.AccountName} ({o.EmailAddress})",
                id = o.UserId.ToString()
            }));
        }

        [Authorize]
        [HttpGet("AutoCompletePage")]
        public ActionResult AutoCompletePage([FromQuery] string? q = null, [FromQuery] bool? showCatchAll = false)
        {
            var pages = _pageService.AutoCompletePage(q).ToList();

            var results = pages.Select(o => new
            {
                text = o.Name,
                id = o.Id.ToString()
            }).ToList();

            if (showCatchAll == true)
            {
                results.Insert(0,
                new
                {
                    text = "*",
                    id = "*"
                });
            }

            return Json(results);
        }

        [Authorize]
        [HttpGet("AutoCompleteNamespace")]
        public ActionResult AutoCompleteNamespace([FromQuery] string? q = null, [FromQuery] bool? showCatchAll = false)
        {
            var namespaces = _pageService.AutoCompleteNamespace(q).ToList();

            if (showCatchAll == true)
            {
                namespaces.Insert(0, "*");
            }

            return Json(namespaces.Select(o => new
            {
                text = o,
                id = o
            }));
        }

        #endregion
    }
}

