using Microsoft.AspNetCore.Mvc;
using TightWiki.Contracts;

namespace TightWiki.ViewHelpers
{
    /// <summary>
    /// Helper methods for building notification redirect URLs.
    /// Replaces the NotifyOf* methods that were on WikiControllerBase and PageModelBase.
    /// </summary>
    public static class NotifyHelper
    {
        public static RedirectResult NotifyOf(string successMessage, string errorMessage, string redirectUrl)
            => new($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(successMessage)}&NotifyErrorMessage={Uri.EscapeDataString(errorMessage)}&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{redirectUrl}")}&RedirectTimeout=5");

        public static RedirectResult NotifyOfSuccess(string message, string redirectUrl)
            => new($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{redirectUrl}")}&RedirectTimeout=5");

        public static RedirectResult NotifyOfWarning(string message, string redirectUrl)
            => new($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyWarningMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{redirectUrl}")}");

        public static RedirectResult NotifyOfError(string message, string redirectUrl)
            => new($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{redirectUrl}")}");

        public static RedirectResult NotifyOfSuccess(string message)
            => new($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(message)}");

        public static RedirectResult NotifyOfWarning(string message)
            => new($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyWarningMessage={Uri.EscapeDataString(message)}");

        public static RedirectResult NotifyOfError(string message)
            => new($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString(message)}");
    }
}
