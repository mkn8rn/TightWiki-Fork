namespace TightWiki.Contracts.DTOs
{
    /// <summary>
    /// Page data optimized for editing forms.
    /// </summary>
    public class PageEditDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Raw wiki markup for editing.
        /// </summary>
        public string Body { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = [];
    }
}
