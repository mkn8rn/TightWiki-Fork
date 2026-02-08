using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TightWiki.Web.Bff.Extensions;
using TightWiki.Web.Bff.Interfaces;
using TightWiki.Web.Bff.ViewModels.Admin;
using TightWiki.Web.Bff.ViewModels.Page;
using TightWiki.Web.Bff.ViewModels.Utility;
using TightWiki.Web.Filters;
using static TightWiki.Contracts.Constants;

namespace TightWiki.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class AdminController(IAdminBffService adminBff) : Controller
    {
        #region Database

        [RequireWikiAdmin]
        [ProducesView]
        [HttpGet("Database")]
        public DatabaseViewModel Database()
            => adminBff.GetDatabaseViewModel();

        [RequireWikiAdmin]
        [HttpPost("Database/{databaseAction}/{database}")]
        public IActionResult Database(DatabaseActionRequest request)
            => adminBff.ExecuteDatabaseAction(request);

        #endregion

        #region Metrics

        [RequireWikiAdmin]
        [ProducesView]
        [HttpGet("Metrics")]
        public MetricsViewModel Metrics()
            => adminBff.GetMetricsViewModel();

        [RequireWikiAdmin]
        [HttpPost("PurgeCompilationStatistics")]
        public IActionResult PurgeCompilationStatistics(ConfirmActionViewModel model)
            => adminBff.PurgeCompilationStatistics(model);

        [RequireWikiAdmin]
        [HttpPost("PurgeMemoryCache")]
        public IActionResult PurgeMemoryCache(ConfirmActionViewModel model)
            => adminBff.PurgeMemoryCache(model);

        #endregion

        #region Compilation Statistics

        [RequireWikiAdmin]
        [ProducesView]
        [HttpGet("CompilationStatistics")]
        public PageCompilationStatisticsViewModel CompilationStatistics(PagedRequest request)
            => adminBff.GetCompilationStatisticsViewModel(request);

        #endregion

        #region Moderate

        [RequireWikiPermission(WikiPermission.Moderate)]
        [ProducesView]
        [HttpGet("Moderate")]
        public PageModerateViewModel Moderate(ModerateRequest request)
            => adminBff.GetModerateViewModel(request);

        #endregion

        #region Missing Pages

        [RequireWikiPermission(WikiPermission.Moderate)]
        [ProducesView]
        [HttpGet("MissingPages")]
        public MissingPagesViewModel MissingPages(PagedRequest request)
            => adminBff.GetMissingPagesViewModel(request);

        #endregion

        #region Namespaces

        [RequireWikiPermission(WikiPermission.Moderate)]
        [ProducesView]
        [HttpGet("Namespaces")]
        public NamespacesViewModel Namespaces(PagedRequest request)
            => adminBff.GetNamespacesViewModel(request);

        [RequireWikiPermission(WikiPermission.Moderate)]
        [ProducesView]
        [HttpGet("Namespace/{namespaceName?}")]
        public NamespaceViewModel Namespace(NamespaceRequest request)
            => adminBff.GetNamespaceViewModel(request);

        #endregion

        #region Pages

        [RequireWikiPermission(WikiPermission.Moderate)]
        [ProducesView]
        [HttpGet("Pages")]
        public PagesViewModel Pages(SearchPagedRequest request)
            => adminBff.GetPagesViewModel(request);

        #endregion

        #region Revisions

        [RequireWikiPermission(WikiPermission.Moderate)]
        [HttpPost("RevertPageRevision/{givenCanonical}/{revision:int}")]
        public IActionResult Revert(ConfirmCanonicalRevisionRequest request)
            => adminBff.RevertPageRevision(request);

        [RequireWikiPermission(WikiPermission.Moderate)]
        [ProducesView]
        [HttpGet("DeletedPageRevisions/{pageId:int}")]
        public DeletedPagesRevisionsViewModel DeletedPageRevisions(int pageId, PagedRequest request)
            => adminBff.GetDeletedPageRevisionsViewModel(pageId, request);

        [RequireWikiPermission(WikiPermission.Moderate)]
        [ProducesView]
        [HttpGet("DeletedPageRevision/{pageId:int}/{revision:int}")]
        public DeletedPageRevisionViewModel DeletedPageRevision(int pageId, int revision)
            => adminBff.GetDeletedPageRevisionViewModel(pageId, revision);

        [RequireWikiPermission(WikiPermission.Moderate)]
        [ProducesView]
        [HttpGet("PageRevisions/{givenCanonical}")]
        public PageRevisionsViewModel PageRevisions(string givenCanonical, PagedRequest request)
            => adminBff.GetPageRevisionsViewModel(givenCanonical, request);

        [RequireWikiPermission(WikiPermission.Moderate)]
        [HttpPost("DeletePageRevision/{givenCanonical}/{revision:int}")]
        public IActionResult DeletePageRevision(ConfirmCanonicalRevisionRequest request)
            => adminBff.DeletePageRevision(request);

        #endregion

        #region Deleted Pages

        [RequireWikiPermission(WikiPermission.Moderate)]
        [ProducesView]
        [HttpGet("DeletedPage/{pageId:int}")]
        public DeletedPageViewModel DeletedPage(int pageId)
            => adminBff.GetDeletedPageViewModel(pageId);

        [RequireWikiPermission(WikiPermission.Moderate)]
        [ProducesView]
        [HttpGet("DeletedPages")]
        public DeletedPagesViewModel DeletedPages(SearchPagedRequest request)
            => adminBff.GetDeletedPagesViewModel(request);

        [RequireWikiPermission(WikiPermission.Moderate)]
        [HttpPost("RebuildAllPages")]
        public IActionResult RebuildAllPages(ConfirmActionViewModel model)
            => adminBff.RebuildAllPages(model);

        [RequireWikiPermission(WikiPermission.Moderate)]
        [HttpPost("PreCacheAllPages")]
        public IActionResult PreCacheAllPages(ConfirmActionViewModel model)
            => adminBff.PreCacheAllPages(model);

        [RequireWikiPermission(WikiPermission.Moderate)]
        [HttpPost("TruncatePageRevisions")]
        public IActionResult TruncatePageRevisions(ConfirmActionViewModel model)
            => adminBff.TruncatePageRevisions(model);

        [RequireWikiPermission(WikiPermission.Moderate)]
        [HttpPost("PurgeDeletedPageRevisions/{pageId:int}")]
        public IActionResult PurgeDeletedPageRevisions(ConfirmPageRequest request)
            => adminBff.PurgeDeletedPageRevisions(request);

        [RequireWikiPermission(WikiPermission.Moderate)]
        [HttpPost("PurgeDeletedPageRevision/{pageId:int}/{revision:int}")]
        public IActionResult PurgeDeletedPageRevision(ConfirmPageRevisionRequest request)
            => adminBff.PurgeDeletedPageRevision(request);

        [RequireWikiPermission(WikiPermission.Moderate)]
        [HttpPost("RestoreDeletedPageRevision/{pageId:int}/{revision:int}")]
        public IActionResult RestoreDeletedPageRevision(ConfirmPageRevisionRequest request)
            => adminBff.RestoreDeletedPageRevision(request);

        [RequireWikiPermission(WikiPermission.Moderate)]
        [HttpPost("PurgeDeletedPages")]
        public IActionResult PurgeDeletedPages(ConfirmActionViewModel model)
            => adminBff.PurgeDeletedPages(model);

        [RequireWikiPermission(WikiPermission.Moderate)]
        [HttpPost("PurgeDeletedPage/{pageId:int}")]
        public IActionResult PurgeDeletedPage(ConfirmPageRequest request)
            => adminBff.PurgeDeletedPage(request);

        [RequireWikiAdmin]
        [HttpPost("DeletePage/{pageId:int}")]
        public IActionResult DeletePage(ConfirmPageRequest request)
            => adminBff.DeletePage(request);

        [RequireWikiPermission(WikiPermission.Moderate)]
        [HttpPost("RestoreDeletedPage/{pageId:int}")]
        public IActionResult RestoreDeletedPage(ConfirmPageRequest request)
            => adminBff.RestoreDeletedPage(request);

        #endregion

        #region Files

        [RequireWikiAdmin]
        [ProducesView]
        [HttpGet("OrphanedPageAttachments")]
        public OrphanedPageAttachmentsViewModel OrphanedPageAttachments(PagedRequest request)
            => adminBff.GetOrphanedPageAttachmentsViewModel(request);

        [RequireWikiAdmin]
        [HttpPost("PurgeOrphanedAttachments")]
        public IActionResult PurgeOrphanedAttachments(ConfirmActionViewModel model)
            => adminBff.PurgeOrphanedAttachments(model);

        [RequireWikiAdmin]
        [HttpPost("PurgeOrphanedAttachment/{pageFileId:int}/{revision:int}")]
        public IActionResult PurgeOrphanedAttachment(PurgeOrphanedAttachmentRequest request)
            => adminBff.PurgeOrphanedAttachment(request);

        #endregion

        #region Menu Items

        [RequireWikiAdmin]
        [ProducesView]
        [HttpGet("MenuItems")]
        public MenuItemsViewModel MenuItems(PagedRequest request)
            => adminBff.GetMenuItemsViewModel(request);

        [RequireWikiAdmin]
        [ProducesView]
        [HttpGet("MenuItem/{id:int?}")]
        public MenuItemViewModel MenuItem(int? id)
            => adminBff.GetMenuItemViewModel(id);

        [RequireWikiAdmin]
        [HttpPost("MenuItem/{id:int?}")]
        public IActionResult MenuItem(SaveMenuItemRequest request)
            => adminBff.SaveMenuItem(request);

        [RequireWikiAdmin]
        [HttpPost("DeleteMenuItem/{id:int}")]
        public IActionResult DeleteMenuItem(DeleteMenuItemRequest request)
            => adminBff.DeleteMenuItem(request);

        #endregion

        #region Config

        [RequireWikiAdmin]
        [ProducesView]
        [HttpGet("Config")]
        public ConfigurationViewModel Config()
            => adminBff.GetConfigViewModel();

        [RequireWikiAdmin]
        [HttpPost("Config")]
        public IActionResult Config(ConfigurationViewModel model)
            => adminBff.SaveConfiguration(model, key => Request.GetFormValue(key, string.Empty));

        #endregion

        #region Emojis

        [RequireWikiPermission(WikiPermission.Moderate)]
        [ProducesView]
        [HttpGet("Emojis")]
        public EmojisViewModel Emojis(SearchPagedRequest request)
            => adminBff.GetEmojisViewModel(request);

        [RequireWikiPermission(WikiPermission.Moderate)]
        [ProducesView]
        [HttpGet("Emoji/{name}")]
        public EmojiViewModel Emoji(string name)
            => adminBff.GetEmojiViewModel(name);

        [RequireWikiAdmin]
        [HttpPost("Emoji/{name}")]
        public IActionResult Emoji(SaveEmojiUploadRequest request)
            => adminBff.SaveEmoji(request);

        [RequireWikiAdmin]
        [ProducesView]
        [HttpGet("AddEmoji")]
        public AddEmojiViewModel AddEmoji()
            => new() { Name = string.Empty, OriginalName = string.Empty, Categories = string.Empty };

        [RequireWikiAdmin]
        [HttpPost("AddEmoji")]
        public IActionResult AddEmoji(CreateEmojiUploadRequest request)
            => adminBff.CreateEmoji(request);

        [RequireWikiAdmin]
        [HttpPost("DeleteEmoji/{name}")]
        public IActionResult DeleteEmoji(DeleteEmojiRequest request)
            => adminBff.DeleteEmoji(request);

        #endregion

        #region Exceptions

        [RequireWikiAdmin]
        [ProducesView]
        [HttpGet("Exceptions")]
        public ExceptionsViewModel Exceptions(PagedRequest request)
            => adminBff.GetExceptionsViewModel(request);

        [RequireWikiAdmin]
        [ProducesView]
        [HttpGet("Exception/{id:int}")]
        public ExceptionViewModel Exception(int id)
            => adminBff.GetExceptionViewModel(id);

        [RequireWikiAdmin]
        [HttpPost("PurgeExceptions")]
        public IActionResult PurgeExceptions(ConfirmActionViewModel model)
            => adminBff.PurgeExceptions(model);

        #endregion

        #region LDAP

        [RequireWikiAdmin]
        [HttpPost("TestLdap")]
        public async Task<IActionResult> TestLdap([FromBody] LdapTestRequest request)
            => await adminBff.TestLdapAsync(request);

        #endregion
    }
}
