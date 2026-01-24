namespace DAL.Models
{
    /// <summary>
    /// Entity model for the PageRevision table in the database.
    /// Stores page revision data including the actual body content.
    /// </summary>
    public class PageRevisionEntity
    {
        public int PageId { get; set; }
        public int Revision { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public int DataHash { get; set; }
        public string ChangeSummary { get; set; } = string.Empty;
        public Guid ModifiedByUserId { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
