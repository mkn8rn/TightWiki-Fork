using Microsoft.AspNetCore.Mvc;
using TightWiki.Web.Bff.ViewModels.Account;

namespace TightWiki.Web.Bff.Interfaces
{
    public interface IAccountBffService
    {
        /// <summary>
        /// Configures the external authentication challenge and returns
        /// a <see cref="ChallengeResult"/> for the specified provider.
        /// </summary>
        IActionResult ExternalLogin(ExternalLoginRequest request);

        /// <summary>
        /// Handles the callback from an external authentication provider.
        /// Orchestrates user lookup, sign-in, profile checks, and link creation.
        /// Throws on failure; the exception middleware handles user-facing errors.
        /// </summary>
        Task<IActionResult> ExternalLoginCallback(ExternalLoginCallbackRequest request);
    }
}
