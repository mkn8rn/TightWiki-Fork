namespace DAL.Models
{
    public class EmojiCategoryDB
    {
        public int EmojiId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string EmojiCount { get; set; } = string.Empty;
    }
}
