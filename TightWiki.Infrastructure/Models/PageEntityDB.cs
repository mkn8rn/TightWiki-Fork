namespace DAL.Models
{
    /// <summary>
    /// Entity model for the Page table in the database.
    /// Stores core page metadata.
    /// </summary>
    public class PageEntityDB
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Revision { get; set; }
        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid ModifiedByUserId { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
