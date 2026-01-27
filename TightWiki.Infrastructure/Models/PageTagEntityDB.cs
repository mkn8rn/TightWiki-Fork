namespace DAL.Models
{
    /// <summary>
    /// Entity model for the PageTag table in the database.
    /// Links pages to their tags.
    /// </summary>
    public class PageTagEntityDB
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public string Tag { get; set; } = string.Empty;
    }
}
