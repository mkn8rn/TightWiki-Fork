namespace TightWiki.Contracts.DataModels
{
    /// <summary>
    /// Emoji category with count for display.
    /// </summary>
    public class EmojiCategory
    {
        public int EmojiId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string EmojiCount { get; set; } = string.Empty;
    }
}
