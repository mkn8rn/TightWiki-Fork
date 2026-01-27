namespace TightWiki.Contracts.Requests
{
    /// <summary>
    /// Request to save/update a page.
    /// </summary>
    public class SavePageRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? ChangeSummary { get; set; }
        public List<string> Tags { get; set; } = [];
    }
}
