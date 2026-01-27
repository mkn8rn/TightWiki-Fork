namespace DAL.Models
{
    /// <summary>
    /// Entity model for the CompilationStatistics table in the database.
    /// Stores per-compilation metrics for wiki pages.
    /// </summary>
    public class CompilationStatisticsEntityDB
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public DateTime CreatedDate { get; set; }
        public double WikifyTimeMs { get; set; }
        public int MatchCount { get; set; }
        public int ErrorCount { get; set; }
        public int OutgoingLinkCount { get; set; }
        public int TagCount { get; set; }
        public int ProcessedBodySize { get; set; }
        public int BodySize { get; set; }
    }
}
