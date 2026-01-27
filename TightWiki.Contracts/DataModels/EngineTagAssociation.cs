namespace TightWiki.Contracts.DataModels
{
    /// <summary>
    /// Tag association with page count for tag cloud display.
    /// </summary>
    public class EngineTagAssociation
    {
        public string Tag { get; set; } = string.Empty;
        public int PageCount { get; set; }
    }
}
