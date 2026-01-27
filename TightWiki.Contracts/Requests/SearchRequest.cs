namespace TightWiki.Contracts.Requests
{
    /// <summary>
    /// Request for searching pages.
    /// </summary>
    public class SearchRequest
    {
        public string Query { get; set; } = string.Empty;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool AllowFuzzyMatching { get; set; } = true;
        public string? Namespace { get; set; }
        public List<string>? Tags { get; set; }
    }
}
