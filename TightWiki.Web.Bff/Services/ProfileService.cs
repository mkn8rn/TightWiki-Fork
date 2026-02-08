using BLL.Services.Configuration;
using BLL.Services.Pages;
using BLL.Services.Security;
using BLL.Services.Users;
using Microsoft.AspNetCore.Hosting;
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
using TightWiki.Localisation;
using TightWiki.Utils;
using TightWiki.Utils.Caching;
using TightWiki.Web.Bff.Extensions;
using TightWiki.Web.Bff.Interfaces;
using TightWiki.Web.Bff.ViewModels.Profile;
using TightWiki.Web.Bff.ViewModels.Utility;
using TightWiki.Web.Engine;
using TightWiki.Web.Engine.Library.Interfaces;
using TightWiki.Web.Engine.Utility;
using static TightWiki.Utils.Images;

namespace TightWiki.Web.Bff.Services
{
    public class ProfileService(
        IConfigurationService configurationService,
        IPageService pageService,
        IUsersService usersService,
        ISecurityService securityService,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IEngineConfigurationProvider configProvider,
        ISessionState session,
        IStringLocalizer<ProfileService> localizer,
        IWebHostEnvironment environment)
        : IProfileBffService
    {
        private readonly IEngineConfiguration _engineConfig = configProvider.GetEngineConfiguration();

        public IActionResult GetAvatar(GetAvatarRequest request)
        {
            session.Page.Name = localizer.Localize("Avatar");

            var avatar = GlobalConfiguration.EnablePublicProfiles
                ? usersService.GetProfileAvatarByNavigation(NamespaceNavigation.CleanAndValidate(request.UserAccountName))
                : null;
            avatar ??= new ProfileAvatar();

            if (avatar.Bytes == null || avatar.Bytes.Length == 0)
            {
                var defaultPath = Path.Combine(environment.WebRootPath, "Avatar.png");
                var image = Image.Load(defaultPath);
                using var ms = new MemoryStream();
                image.SaveAsPng(ms);
                avatar.ContentType = "image/png";
                avatar.Bytes = ms.ToArray();
            }

            if (avatar.Bytes == null || avatar.Bytes.Length == 0)
                throw new FileNotFoundException(localizer.Localize("Avatar not found."));

            if (avatar.ContentType == "image/x-icon")
                return new FileContentResult(avatar.Bytes, avatar.ContentType);

            var img = Image.Load(new MemoryStream(avatar.Bytes));
            int width = img.Width;
            int height = img.Height;

            string givenScale = (request.Scale ?? "").Length == 0 ? "100" : request.Scale!;
            string givenMax = (request.Max ?? "").Length == 0 ? "512" : request.Max!;

            int parsedScale = int.Parse(givenScale);
            int parsedMax = int.Parse(givenMax);

            if (!string.IsNullOrEmpty(request.Exact))
            {
                int parsedExact = Math.Clamp(int.Parse(request.Exact), 16, 1024);
                int diff = img.Width - parsedExact;
                width = img.Width - diff;
                height = img.Height - diff;
                EnsureMinDimensions(ref width, ref height);
            }
            else if (parsedMax != 0 && (img.Width > parsedMax || img.Height > parsedMax))
            {
                int diff = img.Width - parsedMax;
                width = img.Width - diff;
                height = img.Height - diff;
                EnsureMinDimensions(ref width, ref height);
            }
            else if (parsedScale != 100)
            {
                width = (int)(img.Width * (parsedScale / 100.0));
                height = (int)(img.Height * (parsedScale / 100.0));
                EnsureMinDimensions(ref width, ref height);
            }
            else
            {
                return new FileContentResult(avatar.Bytes, avatar.ContentType);
            }

            if (avatar.ContentType.Equals("image/gif", StringComparison.InvariantCultureIgnoreCase))
                return new FileContentResult(ResizeGifImage(avatar.Bytes, width, height), "image/gif");

            using var resized = ResizeImage(img, width, height);
            using var outputMs = new MemoryStream();
            string contentType = BestEffortConvertImage(resized, outputMs, avatar.ContentType);
            return new FileContentResult(outputMs.ToArray(), contentType);
        }

        public PublicViewModel GetPublicViewModel(string userAccountName)
        {
            session.Page.Name = localizer.Localize("Public Profile");

            userAccountName = NamespaceNavigation.CleanAndValidate(userAccountName);

            if (!GlobalConfiguration.EnablePublicProfiles)
                return new PublicViewModel { ErrorMessage = "Public profiles are disabled." };

            var accountProfile = usersService.GetBasicProfileByUserId(
                usersService.GetUserAccountIdByNavigation(userAccountName) ?? Guid.Empty);

            if (accountProfile == null)
                return new PublicViewModel { ErrorMessage = "The specified user was not found." };

            var model = new PublicViewModel
            {
                AccountName = accountProfile.AccountName,
                Navigation = accountProfile.Navigation,
                Id = accountProfile.UserId,
                TimeZone = accountProfile.TimeZone,
                Language = accountProfile.Language,
                Country = accountProfile.Country,
                Biography = WikifierLite.Process(accountProfile.Biography, _engineConfig),
                Avatar = accountProfile.Avatar
            };

            model.RecentlyModified = pageService.GetTopRecentlyModifiedPagesInfoByUserId(
                    accountProfile.UserId, GlobalConfiguration.DefaultProfileRecentlyModifiedCount)
                .OrderByDescending(o => o.ModifiedDate).ThenBy(o => o.Name).ToList();

            foreach (var item in model.RecentlyModified)
            {
                var thisRev = pageService.GetPageRevisionByNavigation(item.Navigation, item.Revision);
                var prevRev = pageService.GetPageRevisionByNavigation(item.Navigation, item.Revision - 1);
                item.ChangeAnalysis = Differentiator.GetComparisonSummary(thisRev?.Body ?? "", prevRev?.Body ?? "");
            }

            return model;
        }

        public AccountProfileViewModel GetMyProfileViewModel()
        {
            session.RequireAuthorizedPermission();
            session.Page.Name = localizer.Localize("My Profile");

            var userId = session.Profile.EnsureNotNull().UserId;
            var model = new AccountProfileViewModel
            {
                AccountProfile = AccountProfileAccountViewModel.FromDataModel(
                    usersService.GetAccountProfileByUserId(userId)),
                Themes = configurationService.GetAllThemes(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll()
            };

            model.AccountProfile.CreatedDate = session.LocalizeDateTime(model.AccountProfile.CreatedDate);
            model.AccountProfile.ModifiedDate = session.LocalizeDateTime(model.AccountProfile.ModifiedDate);

            return model;
        }

        public IActionResult SaveMyProfile(SaveMyProfileRequest request)
        {
            session.RequireAuthorizedPermission();
            session.Page.Name = localizer.Localize("My Profile");

            var model = request.Model;
            var userId = session.Profile.EnsureNotNull().UserId;

            RepopulateDropdowns(model);

            var profile = usersService.GetAccountProfileByUserId(userId);
            model.AccountProfile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName.ToLowerInvariant());

            var error = ValidateAccountNameUniqueness(model, profile);
            if (error != null)
                throw new InvalidOperationException(error);

            if (request.Avatar != null && request.Avatar.Length > 0)
            {
                var avatarError = ProcessAvatarUpload(request.Avatar, profile.UserId);
                if (avatarError != null)
                    throw new InvalidOperationException(avatarError);
            }

            PersistProfileChanges(profile, model.AccountProfile);
            RefreshSignIn(userId, model.AccountProfile);

            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.Navigation]));
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.UserId]));

            session.UserTheme = model.Themes.SingleOrDefault(o => o.Name == model.AccountProfile.Theme)
                ?? GlobalConfiguration.SystemTheme;

            return NotifySuccess(localizer.Localize("Your profile has been saved."),
                $"/Profile/My");
        }

        public IActionResult DeleteMyAccount(DeleteMyAccountRequest request)
        {
            session.RequireAuthorizedPermission();

            if (request.Confirm.UserSelection != true)
                return ConfirmNoRedirect(request.Confirm);

            var userId = session.Profile.EnsureNotNull().UserId;
            var profile = usersService.GetBasicProfileByUserId(userId)
                ?? throw new InvalidOperationException("User not found.");

            var user = userManager.FindByIdAsync(profile.UserId.ToString()).Result
                ?? throw new InvalidOperationException("User not found.");

            var identityResult = userManager.DeleteAsync(user).Result;
            if (!identityResult.Succeeded)
                throw new InvalidOperationException(string.Join("<br />\r\n", identityResult.Errors.Select(o => o.Description)));

            signInManager.SignOutAsync();
            usersService.AnonymizeProfile(profile.UserId);
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.Navigation]));

            return NotifySuccess(localizer.Localize("Your account has been deleted."), "/Profile/Deleted");
        }

        #region Private Helpers

        private static RedirectResult NotifySuccess(string message, string redirectUrl)
            => new($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{redirectUrl}")}&RedirectTimeout=5");

        private static RedirectResult ConfirmNoRedirect(ConfirmActionViewModel model)
            => new($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");

        private void RepopulateDropdowns(AccountProfileViewModel model)
        {
            model.Themes = configurationService.GetAllThemes();
            model.TimeZones = TimeZoneItem.GetAll();
            model.Countries = CountryItem.GetAll();
            model.Languages = LanguageItem.GetAll();
        }

        private string? ValidateAccountNameUniqueness(AccountProfileViewModel model, AccountProfile profile)
        {
            if (!profile.Navigation.Equals(model.AccountProfile.Navigation, StringComparison.InvariantCultureIgnoreCase)
                && usersService.DoesProfileAccountExist(model.AccountProfile.AccountName))
                return "Account name is already in use.";

            return null;
        }

        private void PersistProfileChanges(AccountProfile profile, AccountProfileAccountViewModel accountProfile)
        {
            profile.AccountName = accountProfile.AccountName;
            profile.Navigation = accountProfile.Navigation;
            profile.Biography = accountProfile.Biography;
            profile.ModifiedDate = DateTime.UtcNow;
            usersService.UpdateProfile(profile);
        }

        private void RefreshSignIn(Guid userId, AccountProfileAccountViewModel accountProfile)
        {
            var user = userManager.FindByIdAsync(userId.ToString()).Result;
            if (user == null) return;

            securityService.UpsertUserClaims(userManager, user, BuildProfileClaims(accountProfile));
            signInManager.RefreshSignInAsync(user);
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

        private static List<Claim> BuildProfileClaims(AccountProfileAccountViewModel accountProfile)
        {
            return
            [
                new("timezone", accountProfile.TimeZone),
                new(ClaimTypes.Country, accountProfile.Country),
                new("language", accountProfile.Language),
                new("firstname", accountProfile.FirstName ?? ""),
                new("lastname", accountProfile.LastName ?? ""),
                new("theme", accountProfile.Theme ?? ""),
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

        private static void EnsureMinDimensions(ref int width, ref int height)
        {
            if (height < 16) { int d = 16 - height; height += d; width += d; }
            if (width < 16) { int d = 16 - width; height += d; width += d; }
        }

        #endregion
    }
}
