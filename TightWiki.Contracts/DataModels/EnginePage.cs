using TightWiki.Contracts.Interfaces;

namespace TightWiki.Contracts.DataModels
{
    /// <summary>
    /// Engine-specific page data model that implements IPage for use in wiki transformation.
    /// </summary>
    public class EnginePage : IPage
    {
        public int Id { get; set; }
        public int Revision { get; set; }
        public int MostCurrentRevision { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public string Namespace => Name.Contains("::") 
            ? Name.Substring(0, Name.IndexOf("::")).Trim() 
            : string.Empty;

        public string Title => Name.Contains("::") 
            ? Name.Substring(Name.IndexOf("::") + 2).Trim() 
            : Name;

        public bool IsHistoricalVersion => Revision != MostCurrentRevision;
        public bool Exists => Id > 0;

        // Additional properties used by the engine for search/display
        public decimal Match { get; set; }
        public decimal Weight { get; set; }
        public decimal Score { get; set; }
        public int PaginationPageCount { get; set; }
        public int PaginationPageSize { get; set; }
    }
}
