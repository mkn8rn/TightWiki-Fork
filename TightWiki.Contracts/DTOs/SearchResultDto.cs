namespace TightWiki.Contracts.DTOs
{
    /// <summary>
    /// Search result item.
    /// </summary>
    public class SearchResultDto
    {
        public int PageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public decimal Match { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
