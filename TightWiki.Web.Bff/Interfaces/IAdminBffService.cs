using Microsoft.AspNetCore.Mvc;
using TightWiki.Web.Bff.ViewModels.Admin;
using TightWiki.Web.Bff.ViewModels.Page;
using TightWiki.Web.Bff.ViewModels.Utility;

namespace TightWiki.Web.Bff.Interfaces
{
    public interface IAdminBffService
    {
        // Database
        DatabaseViewModel GetDatabaseViewModel();
        IActionResult ExecuteDatabaseAction(DatabaseActionRequest request);

        // Metrics
        MetricsViewModel GetMetricsViewModel();
        IActionResult PurgeCompilationStatistics(ConfirmActionViewModel model);
        IActionResult PurgeMemoryCache(ConfirmActionViewModel model);

        // Compilation Statistics
        PageCompilationStatisticsViewModel GetCompilationStatisticsViewModel(PagedRequest request);

        // Moderate
        PageModerateViewModel GetModerateViewModel(ModerateRequest request);

        // Missing Pages
        MissingPagesViewModel GetMissingPagesViewModel(PagedRequest request);

        // Namespaces
        NamespacesViewModel GetNamespacesViewModel(PagedRequest request);
        NamespaceViewModel GetNamespaceViewModel(NamespaceRequest request);

        // Pages
        PagesViewModel GetPagesViewModel(SearchPagedRequest request);

        // Revisions
        IActionResult RevertPageRevision(ConfirmCanonicalRevisionRequest request);
        DeletedPagesRevisionsViewModel GetDeletedPageRevisionsViewModel(int pageId, PagedRequest request);
        DeletedPageRevisionViewModel GetDeletedPageRevisionViewModel(int pageId, int revision);
        PageRevisionsViewModel GetPageRevisionsViewModel(string givenCanonical, PagedRequest request);
        IActionResult DeletePageRevision(ConfirmCanonicalRevisionRequest request);

        // Deleted Pages
        DeletedPageViewModel GetDeletedPageViewModel(int pageId);
        DeletedPagesViewModel GetDeletedPagesViewModel(SearchPagedRequest request);
        IActionResult RebuildAllPages(ConfirmActionViewModel model);
        IActionResult PreCacheAllPages(ConfirmActionViewModel model);
        IActionResult TruncatePageRevisions(ConfirmActionViewModel model);
        IActionResult PurgeDeletedPageRevisions(ConfirmPageRequest request);
        IActionResult PurgeDeletedPageRevision(ConfirmPageRevisionRequest request);
        IActionResult RestoreDeletedPageRevision(ConfirmPageRevisionRequest request);
        IActionResult PurgeDeletedPages(ConfirmActionViewModel model);
        IActionResult PurgeDeletedPage(ConfirmPageRequest request);
        IActionResult DeletePage(ConfirmPageRequest request);
        IActionResult RestoreDeletedPage(ConfirmPageRequest request);

        // Files
        OrphanedPageAttachmentsViewModel GetOrphanedPageAttachmentsViewModel(PagedRequest request);
        IActionResult PurgeOrphanedAttachments(ConfirmActionViewModel model);
        IActionResult PurgeOrphanedAttachment(PurgeOrphanedAttachmentRequest request);

        // Menu Items
        MenuItemsViewModel GetMenuItemsViewModel(PagedRequest request);
        MenuItemViewModel GetMenuItemViewModel(int? id);
        IActionResult SaveMenuItem(SaveMenuItemRequest request);
        IActionResult DeleteMenuItem(DeleteMenuItemRequest request);

        // Config
        ConfigurationViewModel GetConfigViewModel();
        IActionResult SaveConfiguration(ConfigurationViewModel model, Func<string, string> getFormValue);

        // Emojis
        EmojisViewModel GetEmojisViewModel(SearchPagedRequest request);
        EmojiViewModel GetEmojiViewModel(string name);
        IActionResult SaveEmoji(SaveEmojiUploadRequest request);
        IActionResult CreateEmoji(CreateEmojiUploadRequest request);
        IActionResult DeleteEmoji(DeleteEmojiRequest request);

        // Exceptions
        ExceptionsViewModel GetExceptionsViewModel(PagedRequest request);
        ExceptionViewModel GetExceptionViewModel(int id);
        IActionResult PurgeExceptions(ConfirmActionViewModel model);

        // LDAP
        Task<IActionResult> TestLdapAsync(LdapTestRequest request);
    }
}
