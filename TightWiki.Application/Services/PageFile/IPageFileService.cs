using TightWiki.Contracts.DataModels;

namespace BLL.Services.PageFile
{
    /// <summary>
    /// Service interface for page file attachment operations.
    /// </summary>
    public interface IPageFileService
    {
        /// <summary>
        /// Detaches a file from a specific page revision.
        /// </summary>
        void DetachPageRevisionAttachment(string pageNavigation, string fileNavigation, int pageRevision);

        /// <summary>
        /// Gets orphaned page attachments (file revisions not linked to any page revision).
        /// </summary>
        List<OrphanedPageAttachment> GetOrphanedPageAttachmentsPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null);

        /// <summary>
        /// Purges all orphaned page attachments.
        /// </summary>
        void PurgeOrphanedPageAttachments();

        /// <summary>
        /// Purges a specific orphaned page attachment.
        /// </summary>
        void PurgeOrphanedPageAttachment(int pageFileId, int revision);

        /// <summary>
        /// Gets page file info by page navigation and optional page revision with pagination.
        /// </summary>
        List<PageFileAttachmentInfo> GetPageFilesInfoByPageNavigationAndPageRevisionPaged(string pageNavigation, int pageNumber, int? pageSize = null, int? pageRevision = null);

        /// <summary>
        /// Gets page file attachment info by page navigation, file navigation, and optional page revision.
        /// </summary>
        PageFileAttachmentInfo? GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null);

        /// <summary>
        /// Gets page file attachment data by page navigation, file navigation, and optional file revision.
        /// </summary>
        PageFileAttachment? GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? fileRevision = null);

        /// <summary>
        /// Gets page file attachment data by page navigation, file navigation, and optional page revision.
        /// </summary>
        PageFileAttachment? GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null);

        /// <summary>
        /// Gets all revisions of a specific file attachment.
        /// </summary>
        List<PageFileAttachmentInfo> GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged(string pageNavigation, string fileNavigation, int pageNumber);

        /// <summary>
        /// Gets all file attachments for a page by page ID.
        /// </summary>
        List<PageFileAttachmentInfo> GetPageFilesInfoByPageId(int pageId);

        /// <summary>
        /// Gets page file info by file navigation within a page.
        /// </summary>
        PageFileRevisionAttachmentInfo? GetPageFileInfoByFileNavigation(int pageId, string fileNavigation);

        /// <summary>
        /// Gets the current page revision's attachment info for a file.
        /// </summary>
        PageFileRevisionAttachmentInfo? GetPageCurrentRevisionAttachmentByFileNavigation(int pageId, string fileNavigation);

        /// <summary>
        /// Creates or updates a page file attachment.
        /// </summary>
        void UpsertPageFile(PageFileAttachment item, Guid userId);

        /// <summary>
        /// Gets the current revision number for a page.
        /// </summary>
        int GetCurrentPageRevision(int pageId);
    }
}
