namespace DAL.Models
{
    /// <summary>
    /// Entity model for the PageFileRevision table in the database.
    /// Stores file revision data including the actual file content.
    /// </summary>
    public class PageFileRevisionEntity
    {
        public int Id { get; set; }
        public int PageFileId { get; set; }
        public int Revision { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid CreatedByUserId { get; set; }
        public byte[] Data { get; set; } = [];
        public int DataHash { get; set; }
    }
}
