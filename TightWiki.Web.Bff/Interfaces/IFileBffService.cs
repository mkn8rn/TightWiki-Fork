using TightWiki.Contracts.DTOs;

namespace TightWiki.Web.Bff.Interfaces
{
    /// <summary>
    /// BFF service for file/attachment operations.
    /// </summary>
    public interface IFileBffService
    {
        /// <summary>
        /// Gets file attachments for a page.
        /// </summary>
        Task<List<FileAttachmentDto>> GetPageAttachmentsAsync(string pageNavigation, int? revision = null);

        /// <summary>
        /// Gets a file's binary content.
        /// </summary>
        Task<FileContentResult?> GetFileContentAsync(string pageNavigation, string fileNavigation, int? revision = null);

        /// <summary>
        /// Gets an image with optional scaling.
        /// </summary>
        Task<FileContentResult?> GetImageAsync(string pageNavigation, string fileNavigation, int? scale = null, int? maxWidth = null, int? revision = null);
    }

    /// <summary>
    /// Result containing file binary data.
    /// </summary>
    public class FileContentResult
    {
        public byte[] Content { get; set; } = [];
        public string ContentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}
