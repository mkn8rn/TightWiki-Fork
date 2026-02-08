namespace TightWiki.Web.Bff.ViewModels.Page
{
    public class DisplayPageResult
    {
        public PageDisplayViewModel Model { get; set; } = new();

        /// <summary>
        /// Page ID for SessionState.SetPageId, null if page was not found.
        /// </summary>
        public int? PageId { get; set; }

        /// <summary>
        /// Custom page title set by engine processing.
        /// </summary>
        public string? PageTitle { get; set; }

        /// <summary>
        /// The page name to set on SessionState.Page.Name (for fallback pages).
        /// </summary>
        public string? OverridePageName { get; set; }

        /// <summary>
        /// Whether the UI should suggest page creation.
        /// </summary>
        public bool? ShouldCreatePage { get; set; }

        public bool PageFound { get; set; }
    }
}
