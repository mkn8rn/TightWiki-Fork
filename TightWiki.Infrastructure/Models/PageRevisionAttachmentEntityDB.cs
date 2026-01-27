namespace DAL.Models
{
    /// <summary>
    /// Entity model for the PageRevisionAttachment junction table.
    /// Links page revisions to file revisions.
    /// </summary>
    public class PageRevisionAttachmentEntityDB
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public int PageFileId { get; set; }
        public int PageRevision { get; set; }
        public int FileRevision { get; set; }
    }
}
