using BLL.Services.Configuration;
using BLL.Services.Pages;
using BLL.Services.Security;
using BLL.Services.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NTDLS.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Security.Claims;
using TightWiki.Contracts;
using TightWiki.Contracts.DataModels;
using TightWiki.Contracts.Interfaces;
using TightWiki.Contracts.Requests;
using TightWiki.Contracts.Responses;
using TightWiki.Localisation;
using TightWiki.Utils;
using TightWiki.Utils.Caching;
using TightWiki.Web.Bff.Extensions;
using TightWiki.Web.Bff.Interfaces;
using TightWiki.Web.Bff.ViewModels.Admin;
using TightWiki.Web.Bff.ViewModels.AdminSecurity;
using TightWiki.Web.Bff.ViewModels.Shared;
using TightWiki.Web.Bff.ViewModels.Utility;

namespace TightWiki.Web.Bff.Services
{
    public class AdminSecurityService(
        IConfigurationService configurationService,
        IPageService pageService,
        IUsersService usersService,
        ISecurityService securityService,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ISessionState session,
        IStringLocalizer<AdminSecurityService> localizer)
        : IAdminSecurityBffService
    {
        #region Roles

        public IActionResult DeleteRole(DeleteRoleRequest request)
        {
            if (request.Confirm.UserSelection != true)
                return ConfirmNoRedirect(request.Confirm);

            usersService.DeleteRole(request.RoleId);
            WikiCache.ClearCategory(WikiCache.Category.Security);
            return ConfirmSuccess(localizer.Localize("The specified role has been deleted."), request.Confirm);
        }

        public IActionResult AddAccountMembership(AddAccountMembershipRequest request)
        {
            if (usersService.IsAccountAMemberOfRole(request.UserId, request.RoleId))
                return new OkObjectResult(ApiResponse<AddAccountMembershipResult>.Ok(new AddAccountMembershipResult { AlreadyExists = true }));

            var result = usersService.AddAccountMembership(request.UserId, request.RoleId);
            WikiCache.ClearCategory(WikiCache.Category.Security);
            return new OkObjectResult(ApiResponse<AddAccountMembershipResult>.Ok(result));
        }

        public IActionResult AddRoleMember(Contracts.Requests.AddRoleMemberRequest request)
        {
            if (usersService.IsAccountAMemberOfRole(request.UserId, request.RoleId))
                return new OkObjectResult(ApiResponse<AddRoleMemberResult>.Ok(new AddRoleMemberResult { AlreadyExists = true }));

            var result = usersService.AddRoleMember(request.UserId, request.RoleId);
            WikiCache.ClearCategory(WikiCache.Category.Security);
            return new OkObjectResult(ApiResponse<AddRoleMemberResult>.Ok(result));
        }

        public IActionResult RemoveRoleMember(RemoveRoleMemberRequest request)
        {
            usersService.RemoveRoleMember(request.RoleId, request.UserId);
            WikiCache.ClearCategory(WikiCache.Category.Security);
            return new OkObjectResult(ApiResponse.Ok());
        }

        public IActionResult RemoveRolePermission(int id)
        {
            usersService.RemoveRolePermission(id);
            WikiCache.ClearCategory(WikiCache.Category.Security);
            return new OkObjectResult(ApiResponse.Ok());
        }

        public IActionResult AddRolePermission(AddRolePermissionRequest request)
        {
            if (usersService.IsRolePermissionDefined(request.RoleId, request.PermissionId, request.PermissionDispositionId, request.Namespace, request.PageId))
                return new OkObjectResult(ApiResponse<InsertRolePermissionResult>.Ok(new InsertRolePermissionResult { AlreadyExists = true }));

            var result = usersService.InsertRolePermission(request.RoleId, request.PermissionId, request.PermissionDispositionId, request.Namespace, request.PageId);
            WikiCache.ClearCategory(WikiCache.Category.Security);
            return new OkObjectResult(ApiResponse<InsertRolePermissionResult>.Ok(result));
        }

        public IActionResult AddRole(AddRoleViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                throw new InvalidOperationException("Role name is required.");

            if (usersService.DoesRoleExist(model.Name))
                throw new InvalidOperationException("Role name is already in use.");

            usersService.InsertRole(model.Name, model.Description);
            WikiCache.ClearCategory(WikiCache.Category.Security);
            return NotifySuccess(localizer.Localize("The role has been created."), "/AdminSecurity/Roles");
        }

        public RoleViewModel GetRoleViewModel(RoleRequest request)
        {
            session.Page.Name = localizer.Localize("Roles");

            var navigation = Navigation.Clean(request.Navigation);
            var role = usersService.GetRoleByName(navigation);

            var members = usersService.GetRoleMembersPaged(role.Id, request.Page_Members, request.OrderBy_Members, request.OrderByDirection_Members);
            var permissions = usersService.GetRolePermissionsPaged(role.Id, request.Page_Permissions, request.OrderBy_Permission, request.OrderByDirection_Permissions);

            return new RoleViewModel
            {
                IsBuiltIn = role.IsBuiltIn,
                Id = role.Id,
                Name = role.Name,
                Members = members,
                AssignedPermissions = permissions,
                PermissionDispositions = usersService.GetAllPermissionDispositions(),
                Permissions = usersService.GetAllPermissions(),
                PaginationPageCount_Members = members.FirstOrDefault()?.PaginationPageCount ?? 0,
                PaginationPageCount_Permissions = permissions.FirstOrDefault()?.PaginationPageCount ?? 0
            };
        }

        public RolesViewModel GetRolesViewModel(PagedRequest request)
        {
            return new RolesViewModel
            {
                Roles = usersService.GetAllRoles(request.OrderBy, request.OrderByDirection)
            };
        }

        public AddRoleViewModel GetAddRoleViewModel()
        {
            session.Page.Name = localizer.Localize("Add Role");
            return new AddRoleViewModel();
        }

        #endregion

        #region Account Roles

        public IActionResult AddAccountPermission(AddAccountPermissionRequest request)
        {
            if (usersService.IsAccountPermissionDefined(request.UserId, request.PermissionId, request.PermissionDispositionId, request.Namespace, request.PageId))
                return new OkObjectResult(ApiResponse<InsertAccountPermissionResult>.Ok(new InsertAccountPermissionResult { AlreadyExists = true }));

            var result = usersService.InsertAccountPermission(request.UserId, request.PermissionId, request.PermissionDispositionId, request.Namespace, request.PageId);
            WikiCache.ClearCategory(WikiCache.Category.Security);
            return new OkObjectResult(ApiResponse<InsertAccountPermissionResult>.Ok(result));
        }

        public IActionResult RemoveAccountPermission(int id)
        {
            usersService.RemoveAccountPermission(id);
            WikiCache.ClearCategory(WikiCache.Category.Security);
            return new OkObjectResult(ApiResponse.Ok());
        }

        public AccountRolesViewModel GetAccountRolesViewModel(AccountRolesRequest request)
        {
            session.Page.Name = localizer.Localize("Roles");

            var navigation = Navigation.Clean(request.Navigation);
            var profile = usersService.GetAccountProfileByNavigation(navigation);

            var memberships = usersService.GetAccountRoleMembershipPaged(profile.UserId, request.Page_Memberships, request.OrderBy_Members, request.OrderByDirection_Memberships);
            var permissions = usersService.GetAccountPermissionsPaged(profile.UserId, request.Page_Permissions, request.OrderBy_Permissions, request.OrderByDirection_Permissions);

            return new AccountRolesViewModel
            {
                Id = profile.UserId,
                AccountName = profile.AccountName,
                Memberships = memberships,
                AssignedPermissions = permissions,
                PermissionDispositions = usersService.GetAllPermissionDispositions(),
                Permissions = usersService.GetAllPermissions(),
                PaginationPageCount_Members = memberships.FirstOrDefault()?.PaginationPageCount ?? 0,
                PaginationPageCount_Permissions = permissions.FirstOrDefault()?.PaginationPageCount ?? 0
            };
        }

        #endregion

        #region Accounts

        public AccountProfileViewModel GetAccountViewModel(string navigation)
        {
            var model = new AccountProfileViewModel
            {
                AccountProfile = AccountProfileAccountViewModel.FromDataModel(
                    usersService.GetAccountProfileByNavigation(Navigation.Clean(navigation))),
                Credential = new CredentialViewModel(),
                Themes = configurationService.GetAllThemes(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Roles = usersService.GetAllRoles()
            };

            model.AccountProfile.CreatedDate = session.LocalizeDateTime(model.AccountProfile.CreatedDate);
            model.AccountProfile.ModifiedDate = session.LocalizeDateTime(model.AccountProfile.ModifiedDate);

            return model;
        }

        public AccountProfileViewModel GetAddAccountViewModel()
        {
            var membershipConfig = configurationService.GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.Membership);
            var defaultSignupRole = membershipConfig.Value<string>("Default Signup Role") ?? string.Empty;
            var customizationConfig = configurationService.GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.Customization);

            return new AccountProfileViewModel
            {
                AccountProfile = new AccountProfileAccountViewModel
                {
                    AccountName = string.Empty,
                    Country = customizationConfig.Value<string>("Default Country", string.Empty),
                    TimeZone = customizationConfig.Value<string>("Default TimeZone", string.Empty),
                    Language = customizationConfig.Value<string>("Default Language", string.Empty)
                },
                DefaultRole = defaultSignupRole,
                Themes = configurationService.GetAllThemes(),
                Credential = new CredentialViewModel(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Roles = usersService.GetAllRoles()
            };
        }

        public AccountsViewModel GetAccountsViewModel(SearchPagedRequest request)
        {
            var users = usersService.GetAllUsersPaged(request.Page, request.OrderBy, request.OrderByDirection, request.SearchString);

            users?.ForEach(o =>
            {
                o.CreatedDate = session.LocalizeDateTime(o.CreatedDate);
                o.ModifiedDate = session.LocalizeDateTime(o.ModifiedDate);
            });

            return new AccountsViewModel
            {
                Users = users,
                SearchString = request.SearchString,
                PaginationPageCount = users.FirstOrDefault()?.PaginationPageCount ?? 0
            };
        }

        public IActionResult SaveAccount(SaveAccountRequest request)
        {
            var model = request.Model;
            RepopulateDropdowns(model);
            model.AccountProfile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName.ToLowerInvariant());

            var user = ResolveIdentityUser(model.AccountProfile.UserId)
                ?? throw new InvalidOperationException("User not found.");

            var passwordError = TryResetPassword(user, model);
            if (passwordError != null)
                throw new InvalidOperationException(passwordError);

            var profile = usersService.GetAccountProfileByUserId(model.AccountProfile.UserId);

            var uniquenessError = ValidateAccountUniqueness(model, profile);
            if (uniquenessError != null)
                throw new InvalidOperationException(uniquenessError);

            if (request.Avatar != null && request.Avatar.Length > 0)
            {
                var avatarError = ProcessAvatarUpload(request.Avatar, profile.UserId);
                if (avatarError != null)
                    throw new InvalidOperationException(avatarError);
            }

            PersistProfileChanges(profile, model.AccountProfile);
            UpsertClaims(user, model.AccountProfile);
            RefreshSessionIfSelf(user, profile, model.AccountProfile);
            ApplyEmailChanges(user, profile, model.AccountProfile);

            WikiCache.ClearCategory(WikiCache.Category.Security);
            return NotifySuccess(localizer.Localize("Your profile has been saved successfully!"),
                $"/AdminSecurity/Account/{model.AccountProfile.Navigation}");
        }

        public IActionResult CreateAccount(CreateAccountRequest request)
        {
            var model = request.Model;
            RepopulateDropdowns(model);
            model.AccountProfile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName?.ToLowerInvariant());

            var validationError = ValidateNewAccount(model);
            if (validationError != null)
                throw new InvalidOperationException(validationError);

            var identityUser = ProvisionIdentityUser(model)
                ?? throw new InvalidOperationException(model.ErrorMessage ?? "Failed to create user.");

            var userId = Guid.Parse(identityUser.Id);
            UpsertClaims(identityUser, model.AccountProfile);

            usersService.CreateProfile(userId, model.AccountProfile.AccountName!);
            usersService.AddRoleMemberByName(userId, model.DefaultRole);

            var profile = usersService.GetAccountProfileByUserId(userId);
            PersistProfileChanges(profile, model.AccountProfile);

            if (request.Avatar != null && request.Avatar.Length > 0)
                ProcessAvatarUpload(request.Avatar, userId);

            return NotifySuccess(localizer.Localize("The account has been created."),
                $"/AdminSecurity/Account/{profile.Navigation}");
        }

        public IActionResult DeleteAccount(DeleteAccountRequest request)
        {
            if (request.Confirm.UserSelection != true)
                return ConfirmNoRedirect(request.Confirm);

            var profile = usersService.GetAccountProfileByNavigation(request.Navigation);
            var user = userManager.FindByIdAsync(profile.UserId.ToString()).Result
                ?? throw new InvalidOperationException("User not found.");

            var identityResult = userManager.DeleteAsync(user).Result;
            if (!identityResult.Succeeded)
                throw new InvalidOperationException(string.Join("<br />\r\n", identityResult.Errors.Select(o => o.Description)));

            usersService.AnonymizeProfile(profile.UserId);
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.Navigation]));
            WikiCache.ClearCategory(WikiCache.Category.Security);

            if (profile.UserId == session.Profile?.UserId)
            {
                signInManager.SignOutAsync();
                return NotifySuccess(localizer.Localize("Your account has been deleted."), "/Profile/Deleted");
            }

            return ConfirmSuccess(localizer.Localize("The account has been deleted."), request.Confirm);
        }

        #endregion

        #region AutoComplete

        public IActionResult AutoCompleteRole(string? q)
            => new JsonResult(usersService.AutoCompleteRole(q)
                .Select(o => new { text = o.Name, id = o.Id.ToString() }).ToList());

        public IActionResult AutoCompleteAccount(string? q)
            => new JsonResult(usersService.AutoCompleteAccount(q)
                .Select(o => new
                {
                    text = string.IsNullOrWhiteSpace(o.EmailAddress) ? o.AccountName : $"{o.AccountName} ({o.EmailAddress})",
                    id = o.UserId.ToString()
                }).ToList());

        public IActionResult AutoCompletePage(string? q, bool? showCatchAll)
        {
            var results = pageService.AutoCompletePage(q).Select(o => new { text = o.Name, id = o.Id.ToString() }).ToList<object>();
            if (showCatchAll == true)
                results.Insert(0, new { text = "*", id = "*" });
            return new JsonResult(results);
        }

        public IActionResult AutoCompleteNamespace(string? q, bool? showCatchAll)
        {
            var namespaces = pageService.AutoCompleteNamespace(q).ToList();
            if (showCatchAll == true)
                namespaces.Insert(0, "*");
            return new JsonResult(namespaces.Select(o => new { text = o, id = o }).ToList());
        }

        #endregion

        #region Private Helpers

        private static RedirectResult NotifySuccess(string message, string redirectUrl)
            => new($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{redirectUrl}")}&RedirectTimeout=5");

        private static RedirectResult ConfirmSuccess(string message, ConfirmActionViewModel model)
            => new($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{model.YesRedirectURL}")}&RedirectTimeout=5");

        private static RedirectResult ConfirmNoRedirect(ConfirmActionViewModel model)
            => new($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");

        private IdentityUser? ResolveIdentityUser(Guid userId)
            => userManager.FindByIdAsync(userId.ToString()).Result;

        private string? TryResetPassword(IdentityUser user, AccountProfileViewModel model)
        {
            if (model.Credential.Password == null
                || model.Credential.Password == CredentialViewModel.NOTSET
                || model.Credential.Password != model.Credential.ComparePassword)
                return null;

            var token = userManager.GeneratePasswordResetTokenAsync(user).Result;
            if (token == null) return "Could not generate password reset token.";

            var result = userManager.ResetPasswordAsync(user, token, model.Credential.Password).Result;
            if (result == null || !result.Succeeded)
                return string.Join("<br />\r\n", result?.Errors.Select(o => o.Description) ?? []);

            if (model.AccountProfile.AccountName.Equals(Constants.DEFAULTACCOUNT, StringComparison.InvariantCultureIgnoreCase))
                usersService.SetAdminPasswordIsChanged();

            return null;
        }

        private string? ValidateAccountUniqueness(AccountProfileViewModel model, AccountProfile profile)
        {
            if (!profile.Navigation.Equals(model.AccountProfile.Navigation, StringComparison.InvariantCultureIgnoreCase)
                && usersService.DoesProfileAccountExist(model.AccountProfile.AccountName))
                return "Account name is already in use.";

            if (!profile.EmailAddress.Equals(model.AccountProfile.EmailAddress, StringComparison.InvariantCultureIgnoreCase)
                && usersService.DoesEmailAddressExist(model.AccountProfile.EmailAddress))
                return "Email address is already in use.";

            return null;
        }

        private string? ValidateNewAccount(AccountProfileViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.AccountProfile.AccountName))
                return "Account name is required.";
            if (usersService.DoesProfileAccountExist(model.AccountProfile.AccountName))
                return "Account name is already in use.";
            if (usersService.DoesEmailAddressExist(model.AccountProfile.EmailAddress))
                return "Email address is already in use.";
            return null;
        }

        private IdentityUser? ProvisionIdentityUser(AccountProfileViewModel model)
        {
            var identityUser = new IdentityUser(model.AccountProfile.EmailAddress)
            {
                Email = model.AccountProfile.EmailAddress,
                EmailConfirmed = true
            };

            var result = userManager.CreateAsync(identityUser, model.Credential.Password).Result;
            if (!result.Succeeded)
            {
                model.ErrorMessage = string.Join("<br />\r\n", result.Errors.Select(o => o.Description));
                return null;
            }

            identityUser = userManager.FindByEmailAsync(model.AccountProfile.EmailAddress).Result;
            if (identityUser == null)
                model.ErrorMessage = "Failed to find created user.";

            return identityUser;
        }

        private void PersistProfileChanges(AccountProfile profile, AccountProfileAccountViewModel accountProfile)
        {
            profile.AccountName = accountProfile.AccountName;
            profile.Navigation = accountProfile.Navigation;
            profile.Biography = accountProfile.Biography;
            profile.ModifiedDate = DateTime.UtcNow;
            usersService.UpdateProfile(profile);
        }

        private void UpsertClaims(IdentityUser user, AccountProfileAccountViewModel accountProfile)
            => securityService.UpsertUserClaims(userManager, user, BuildProfileClaims(accountProfile));

        private void RefreshSessionIfSelf(IdentityUser user, AccountProfile profile, AccountProfileAccountViewModel accountProfile)
        {
            if (session.Profile.EnsureNotNull().UserId != accountProfile.UserId)
                return;

            signInManager.RefreshSignInAsync(user);
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.Navigation]));
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.UserId]));
            session.UserTheme = configurationService.GetAllThemes()
                .SingleOrDefault(o => o.Name == accountProfile.Theme) ?? GlobalConfiguration.SystemTheme;
        }

        private void ApplyEmailChanges(IdentityUser user, AccountProfile profile, AccountProfileAccountViewModel accountProfile)
        {
            bool emailConfirmChanged = profile.EmailConfirmed != accountProfile.EmailConfirmed;
            if (emailConfirmChanged)
            {
                user.EmailConfirmed = accountProfile.EmailConfirmed;
                var ur = userManager.UpdateAsync(user).Result;
                if (!ur.Succeeded) throw new Exception(string.Join("<br />\r\n", ur.Errors.Select(o => o.Description)));
            }

            if (!profile.EmailAddress.Equals(accountProfile.EmailAddress, StringComparison.InvariantCultureIgnoreCase))
            {
                bool wasEmailAlreadyConfirmed = user.EmailConfirmed;
                var ser = userManager.SetEmailAsync(user, accountProfile.EmailAddress).Result;
                if (!ser.Succeeded) throw new Exception(string.Join("<br />\r\n", ser.Errors.Select(o => o.Description)));
                var sur = userManager.SetUserNameAsync(user, accountProfile.EmailAddress).Result;
                if (!sur.Succeeded) throw new Exception(string.Join("<br />\r\n", sur.Errors.Select(o => o.Description)));
                if (wasEmailAlreadyConfirmed && !emailConfirmChanged)
                {
                    user.EmailConfirmed = true;
                    var ur = userManager.UpdateAsync(user).Result;
                    if (!ur.Succeeded) throw new Exception(string.Join("<br />\r\n", ur.Errors.Select(o => o.Description)));
                }
            }
        }

        private void RepopulateDropdowns(AccountProfileViewModel model)
        {
            model.Themes = configurationService.GetAllThemes();
            model.TimeZones = TimeZoneItem.GetAll();
            model.Countries = CountryItem.GetAll();
            model.Languages = LanguageItem.GetAll();
            model.Roles = usersService.GetAllRoles();
        }

        private string? ProcessAvatarUpload(IFormFile avatarFile, Guid userId)
        {
            if (avatarFile.ContentType == null || !GlobalConfiguration.AllowableImageTypes.Contains(avatarFile.ContentType.ToLowerInvariant()))
                return "Could not save the attached image, type not allowed.";
            if (avatarFile.Length > GlobalConfiguration.MaxAvatarFileSize)
                return "Could not save the attached image, too large.";
            try
            {
                using var stream = avatarFile.OpenReadStream();
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                var image = CropImageToCenteredSquare(ms);
                usersService.UpdateProfileAvatar(userId, image, "image/webp");
                return null;
            }
            catch
            {
                return "Could not save the attached image.";
            }
        }

        private static List<Claim> BuildProfileClaims(AccountProfileAccountViewModel p)
        {
            return
            [
                new("timezone", p.TimeZone),
                new(ClaimTypes.Country, p.Country),
                new("language", p.Language),
                new("firstname", p.FirstName ?? ""),
                new("lastname", p.LastName ?? ""),
                new("theme", p.Theme ?? ""),
            ];
        }

        private static byte[] CropImageToCenteredSquare(MemoryStream inputStream)
        {
            using var image = Image.Load(inputStream);
            if (image.Width != image.Height)
            {
                int size = Math.Min(image.Width, image.Height);
                int x = (image.Width - size) / 2;
                int y = (image.Height - size) / 2;
                image.Mutate(ctx => ctx.Crop(new Rectangle(x, y, size, size)));
            }
            using var outputStream = new MemoryStream();
            image.SaveAsWebp(outputStream);
            return outputStream.ToArray();
        }

        #endregion
    }
}
