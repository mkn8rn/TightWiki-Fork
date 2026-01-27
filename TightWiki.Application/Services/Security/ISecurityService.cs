using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace BLL.Services.Security
{
    /// <summary>
    /// Service interface for security and authentication operations.
    /// </summary>
    public interface ISecurityService
    {
        /// <summary>
        /// Validates encryption setup and creates the admin user if this is the first run.
        /// </summary>
        Task ValidateEncryptionAndCreateAdminUserAsync(UserManager<IdentityUser> userManager);

        /// <summary>
        /// Upserts user claims (removes existing claims of the same type and adds new ones).
        /// </summary>
        Task UpsertUserClaimsAsync(UserManager<IdentityUser> userManager, IdentityUser user, List<Claim> claims);

        /// <summary>
        /// Synchronous wrapper for UpsertUserClaimsAsync for backwards compatibility.
        /// </summary>
        void UpsertUserClaims(UserManager<IdentityUser> userManager, IdentityUser user, List<Claim> claims);
    }
}
