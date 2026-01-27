namespace TightWiki.Contracts.DTOs
{
    /// <summary>
    /// Core page data - used for data operations (create, update, export).
    /// </summary>
    public class PageDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public int Revision { get; set; }
        public int MostCurrentRevision { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public string ModifiedByUserName { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = [];
    }
}
