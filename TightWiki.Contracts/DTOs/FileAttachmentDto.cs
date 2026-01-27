namespace TightWiki.Contracts.DTOs
{
    /// <summary>
    /// File attachment metadata.
    /// </summary>
    public class FileAttachmentDto
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FileNavigation { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public string FriendlySize { get; set; } = string.Empty;
        public int Revision { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
