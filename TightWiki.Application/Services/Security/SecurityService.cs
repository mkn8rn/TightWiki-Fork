using BLL.Services.Configuration;
using BLL.Services.Users;
using Microsoft.AspNetCore.Identity;
using NTDLS.Helpers;
using System.Security.Claims;
using TightWiki.Utils;
using static TightWiki.Contracts.Constants;

namespace BLL.Services.Security
{
    /// <summary>
    /// Business logic service for security and authentication operations.
    /// </summary>
    public sealed class SecurityService : ISecurityService
    {
        private readonly IConfigurationService _configurationService;
        private readonly IUsersService _usersService;

        public SecurityService(
            IConfigurationService configurationService,
            IUsersService usersService)
        {
            _configurationService = configurationService;
            _usersService = usersService;
        }

        /// <summary>
        /// Validates encryption setup and creates the admin user if this is the first run.
        /// Adds the first user with the email and password contained in Constants.DEFAULTUSERNAME and Constants.DEFAULTPASSWORD.
        /// </summary>
        public async Task ValidateEncryptionAndCreateAdminUserAsync(UserManager<IdentityUser> userManager)
        {
            if (_configurationService.IsFirstRun())
            {
                // If this is the first time the app has run on this machine (based on an encryption key) 
                // then clear the admin password status.
                // This will cause the application to set the admin password to the default password 
                // and display a warning until it is changed.
                _usersService.SetAdminPasswordClear();
            }

            if (_usersService.GetAdminPasswordStatus() == AdminPasswordChangeState.NeedsToBeSet)
            {
                var user = await userManager.FindByNameAsync(DEFAULTUSERNAME);
                if (user == null)
                {
                    var creationResult = await userManager.CreateAsync(new IdentityUser(DEFAULTUSERNAME), DEFAULTPASSWORD);
                    if (!creationResult.Succeeded)
                    {
                        throw new System.Exception(string.Join("\r\n", creationResult.Errors.Select(o => o.Description)));
                    }

                    user = await userManager.FindByNameAsync(DEFAULTUSERNAME);
                }

                user.EnsureNotNull();

                user.Email = DEFAULTUSERNAME; // Ensure email is set or updated
                user.EmailConfirmed = true;
                var emailUpdateResult = await userManager.UpdateAsync(user);
                if (!emailUpdateResult.Succeeded)
                {
                    throw new System.Exception(string.Join("\r\n", emailUpdateResult.Errors.Select(o => o.Description)));
                }

                var membershipConfig = _configurationService.GetConfigurationEntriesByGroupName(ConfigurationGroup.Membership);
                var customizationConfig = _configurationService.GetConfigurationEntriesByGroupName(ConfigurationGroup.Customization);

                var claimsToAdd = new List<Claim>
                {
                    new(ClaimTypes.Role, "Administrator"),
                    new("timezone", membershipConfig.Value<string>("Default TimeZone").EnsureNotNull()),
                    new(ClaimTypes.Country, membershipConfig.Value<string>("Default Country").EnsureNotNull()),
                    new("language", membershipConfig.Value<string>("Default Language").EnsureNotNull()),
                    new("theme", customizationConfig.Value<string>("Theme").EnsureNotNull()),
                };

                await UpsertUserClaimsAsync(userManager, user, claimsToAdd);

                var token = await userManager.GeneratePasswordResetTokenAsync(user.EnsureNotNull());
                var result = await userManager.ResetPasswordAsync(user, token, DEFAULTPASSWORD);
                if (!result.Succeeded)
                {
                    throw new System.Exception(string.Join("\r\n", result.Errors.Select(o => o.Description)));
                }

                _usersService.SetAdminPasswordIsDefault();

                var userId = Guid.Parse(user.Id);
                var existingProfileUserId = _usersService.GetUserAccountIdByNavigation(Navigation.Clean(DEFAULTACCOUNT));
                if (existingProfileUserId == null)
                {
                    _usersService.CreateProfile(userId, DEFAULTACCOUNT);
                }
                else
                {
                    _usersService.SetProfileUserId(DEFAULTACCOUNT, userId);
                }

                // Add admin user to the Administrator role in AccountRoles table
                // This is necessary for the permission system to recognize them as an admin
                _usersService.AddRoleMemberByName(userId, "Administrator");
            }
        }

        /// <summary>
        /// Upserts user claims - removes existing claims of the same type and adds new ones.
        /// </summary>
        public async Task UpsertUserClaimsAsync(UserManager<IdentityUser> userManager, IdentityUser user, List<Claim> claims)
        {
            // Get existing claims for the user
            var existingClaims = await userManager.GetClaimsAsync(user);

            foreach (var claim in claims)
            {
                // Remove existing claims if they exist
                var existingClaim = existingClaims.FirstOrDefault(c => c.Type == claim.Type);
                if (existingClaim != null)
                {
                    await userManager.RemoveClaimAsync(user, existingClaim);
                }

                // Add new claim
                await userManager.AddClaimAsync(user, claim);
            }

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new System.Exception(string.Join("<br />\r\n", result.Errors.Select(o => o.Description)));
            }
        }

        /// <summary>
        /// Synchronous wrapper for UpsertUserClaimsAsync for backwards compatibility.
        /// </summary>
        public void UpsertUserClaims(UserManager<IdentityUser> userManager, IdentityUser user, List<Claim> claims)
        {
            UpsertUserClaimsAsync(userManager, user, claims).GetAwaiter().GetResult();
        }
    }
}
