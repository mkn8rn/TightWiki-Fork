using Microsoft.AspNetCore.Identity;
using NTDLS.Helpers;
using System.Security.Claims;
using TightWiki.Library;

namespace TightWiki.Repository
{
    public interface ISecurityRepository
    {
        Task ValidateEncryptionAndCreateAdminUserAsync(UserManager<IdentityUser> userManager);
        Task UpsertUserClaimsAsync(UserManager<IdentityUser> userManager, IdentityUser user, List<Claim> givenClaims);
    }


    public static class SecurityRepository
    {
        private static IServiceProvider? _serviceProvider;
        public static void UseServiceProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        private static ISecurityRepository Repo =>
            _serviceProvider?.GetService(typeof(ISecurityRepository)) as ISecurityRepository
            ?? throw new InvalidOperationException("ISecurityRepository is not configured.");

        /// <summary>
        /// Detect whether this is the first time the WIKI has ever been run and do some initialization.
        /// Adds the first user with the email and password contained in Constants.DEFAULTUSERNAME and Constants.DEFAULTPASSWORD
        /// </summary>
        public static async Task ValidateEncryptionAndCreateAdminUserAsync(UserManager<IdentityUser> userManager)
            => await Repo.ValidateEncryptionAndCreateAdminUserAsync(userManager);

        public static async Task UpsertUserClaimsAsync(UserManager<IdentityUser> userManager, IdentityUser user, List<Claim> givenClaims)
            => await Repo.UpsertUserClaimsAsync(userManager, user, givenClaims);

        /// <summary>
        /// Synchronous wrapper for backwards compatibility. Prefer UpsertUserClaimsAsync.
        /// </summary>
        public static void UpsertUserClaims(UserManager<IdentityUser> userManager, IdentityUser user, List<Claim> givenClaims)
            => Repo.UpsertUserClaimsAsync(userManager, user, givenClaims).GetAwaiter().GetResult();
    }

    public sealed class SecurityRepositoryEf : ISecurityRepository
    {
        public SecurityRepositoryEf()
        {
        }

        /// <summary>
        /// Detect whether this is the first time the WIKI has ever been run and do some initialization.
        /// Adds the first user with the email and password contained in Constants.DEFAULTUSERNAME and Constants.DEFAULTPASSWORD
        /// </summary>
        public async Task ValidateEncryptionAndCreateAdminUserAsync(UserManager<IdentityUser> userManager)
        {
            if (ConfigurationRepository.IsFirstRun())
            {
                //If this is the first time the app has run on this machine (based on an encryption key) then clear the admin password status.
                //This will cause the application to set the admin password to the default password and display a warning until it is changed.
                UsersRepository.SetAdminPasswordClear();
            }

            if (UsersRepository.AdminPasswordStatus() == Constants.AdminPasswordChangeState.NeedsToBeSet)
            {
                var user = await userManager.FindByNameAsync(Constants.DEFAULTUSERNAME);
                if (user == null)
                {
                    var creationResult = await userManager.CreateAsync(new IdentityUser(Constants.DEFAULTUSERNAME), Constants.DEFAULTPASSWORD);
                    if (!creationResult.Succeeded)
                    {
                        throw new Exception(string.Join("\r\n", creationResult.Errors.Select(o => o.Description)));
                    }

                    user = await userManager.FindByNameAsync(Constants.DEFAULTUSERNAME);
                }

                user.EnsureNotNull();

                user.Email = Constants.DEFAULTUSERNAME; // Ensure email is set or updated
                user.EmailConfirmed = true;
                var emailUpdateResult = await userManager.UpdateAsync(user);
                if (!emailUpdateResult.Succeeded)
                {
                    throw new Exception(string.Join("\r\n", emailUpdateResult.Errors.Select(o => o.Description)));
                }

                var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Membership);
                var customizationConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Customization);

                var claimsToAdd = new List<Claim>
                    {
                        new (ClaimTypes.Role, "Administrator"),
                        new ("timezone", membershipConfig.Value<string>("Default TimeZone").EnsureNotNull()),
                        new (ClaimTypes.Country, membershipConfig.Value<string>("Default Country").EnsureNotNull()),
                        new ("language", membershipConfig.Value<string>("Default Language").EnsureNotNull()),
                        new ("theme", customizationConfig.Value<string>("Theme").EnsureNotNull()),
                    };

                await UpsertUserClaimsAsync(userManager, user, claimsToAdd);

                var token = await userManager.GeneratePasswordResetTokenAsync(user.EnsureNotNull());
                var result = await userManager.ResetPasswordAsync(user, token, Constants.DEFAULTPASSWORD);
                if (!result.Succeeded)
                {
                    throw new Exception(string.Join("\r\n", emailUpdateResult.Errors.Select(o => o.Description)));
                }

                UsersRepository.SetAdminPasswordIsDefault();

                var userId = Guid.Parse(user.Id);
                var existingProfileUserId = UsersRepository.GetUserAccountIdByNavigation(Navigation.Clean(Constants.DEFAULTACCOUNT));
                if (existingProfileUserId == null)
                {
                    UsersRepository.CreateProfile(userId, Constants.DEFAULTACCOUNT);
                }
                else
                {
                    UsersRepository.SetProfileUserId(Constants.DEFAULTACCOUNT, userId);
                }

                // Add admin user to the Administrator role in AccountRoles table
                // This is necessary for the permission system to recognize them as an admin
                UsersRepository.AddRoleMemberByname(userId, "Administrator");
            }
        }

        public async Task UpsertUserClaimsAsync(UserManager<IdentityUser> userManager, IdentityUser user, List<Claim> givenClaims)
        {
            // Get existing claims for the user
            var existingClaims = await userManager.GetClaimsAsync(user);

            foreach (var givenClaim in givenClaims)
            {
                // Remove existing claims if they exist
                var firstNameClaim = existingClaims.FirstOrDefault(c => c.Type == givenClaim.Type);
                if (firstNameClaim != null)
                {
                    await userManager.RemoveClaimAsync(user, firstNameClaim);
                }

                // Add new claim.
                await userManager.AddClaimAsync(user, givenClaim);
            }

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join("<br />\r\n", result.Errors.Select(o => o.Description)));
            }
        }
    }
}
