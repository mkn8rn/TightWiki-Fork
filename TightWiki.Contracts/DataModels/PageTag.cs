namespace TightWiki.Contracts.DataModels
{
    public class PageTag
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public string Tag { get; set; } = string.Empty;
    }
}
