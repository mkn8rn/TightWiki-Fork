namespace TightWiki.Contracts.DataModels
{
    /// <summary>
    /// Related page information for similar/related page listings.
    /// </summary>
    public class EngineRelatedPage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Matches { get; set; }
        public int PaginationPageCount { get; set; }
        public int PaginationPageSize { get; set; }

        public string Title => Name.Contains("::")
            ? Name.Substring(Name.IndexOf("::") + 2).Trim()
            : Name;
    }
}
