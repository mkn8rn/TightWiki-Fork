namespace TightWiki.Contracts.DataModels
{
    /// <summary>
    /// Page revision metadata for revision history display.
    /// </summary>
    public class EnginePageRevision
    {
        public int PageId { get; set; }
        public int Revision { get; set; }
        public int HighestRevision { get; set; }
        public int HigherRevisionCount { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ChangeSummary { get; set; } = string.Empty;
        public string ChangeAnalysis { get; set; } = string.Empty;
        public Guid ModifiedByUserId { get; set; }
        public string ModifiedByUserName { get; set; } = string.Empty;
        public DateTime ModifiedDate { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public int PaginationPageCount { get; set; }
        public int PaginationPageSize { get; set; }
    }
}
