namespace DAL.Models
{
    /// <summary>
    /// Entity model for the PageToken table in the database.
    /// Stores search tokens for full-text search functionality.
    /// </summary>
    public class PageTokenEntity
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public string Token { get; set; } = string.Empty;
        public string DoubleMetaphone { get; set; } = string.Empty;
        public double Weight { get; set; }
    }
}
