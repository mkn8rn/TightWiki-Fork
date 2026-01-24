namespace DAL.Models
{
    /// <summary>
    /// Entity model for the PageComment table in the database.
    /// Stores user comments on wiki pages.
    /// </summary>
    public class PageCommentEntity
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public Guid UserId { get; set; }
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
