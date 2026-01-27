namespace TightWiki.Contracts.DataModels
{
    /// <summary>
    /// Search token for full-text search indexing.
    /// </summary>
    public class EnginePageToken
    {
        public int PageId { get; set; }
        public string Token { get; set; } = string.Empty;
        public string DoubleMetaphone { get; set; } = string.Empty;
        public double Weight { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is EnginePageToken other
                && PageId == other.PageId
                && string.Equals(Token, other.Token, StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PageId, Token.ToLowerInvariant());
        }
    }
}
