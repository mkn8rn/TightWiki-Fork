namespace DAL.Models
{
    /// <summary>
    /// Entity model for the PageFile table in the database.
    /// Stores file attachment metadata.
    /// </summary>
    public class PageFileEntityDB
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public int Revision { get; set; }
    }
}
