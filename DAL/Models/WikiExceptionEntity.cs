namespace DAL.Models
{
    /// <summary>
    /// Entity model for the WikiException table in the database.
    /// Stores application exceptions for troubleshooting.
    /// </summary>
    public class WikiExceptionEntity
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string ExceptionText { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
