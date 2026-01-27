namespace DAL.Models
{
    /// <summary>
    /// Entity model for the PageReference table in the database.
    /// Stores inter-page links/references.
    /// </summary>
    public class PageReferenceEntityDB
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public string ReferencesPageNavigation { get; set; } = string.Empty;
        public int? ReferencesPageId { get; set; }
    }
}
