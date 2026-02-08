using Microsoft.AspNetCore.Http;
using TightWiki.Contracts.DataModels;
using static TightWiki.Contracts.Constants;

namespace TightWiki.Contracts.Interfaces
{
    public interface ISessionState
    {
        // Identity
        bool IsAuthenticated { get; }
        bool IsAdministrator { get; }
        IAccountProfile? Profile { get; set; }
        Theme UserTheme { get; set; }
        List<ApparentPermission> Permissions { get; }

        // Current page context
        IPage Page { get; set; }
        string? PageTitle { get; set; }
        bool ShouldCreatePage { get; set; }
        string PageNavigation { get; set; }
        string PageNavigationEscaped { get; set; }
        string PageTags { get; set; }
        ProcessingInstructionCollection PageInstructions { get; set; }

        IQueryCollection? QueryString { get; set; }

        void EnsureInitialized();
        void SetPageId(int? pageId, int? revision = null);

        // Permissions
        bool HoldsPermission(WikiPermission[] permissions);
        bool HoldsPermission(WikiPermission permission);
        bool HoldsPermission(string? givenCanonical, WikiPermission permission);
        bool HoldsPermission(string? givenCanonical, WikiPermission[] permissions);
        void RequirePermission(string? givenCanonical, WikiPermission permission);
        void RequirePermission(string? givenCanonical, WikiPermission[] permissions);
        void RequireAdminPermission();
        void RequireAuthorizedPermission();

        // Localization
        DateTime LocalizeDateTime(DateTime datetime);
        TimeZoneInfo GetPreferredTimeZone();
    }
}
