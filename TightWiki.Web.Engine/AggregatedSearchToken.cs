namespace TightWiki.Web.Engine
{
    public class AggregatedSearchToken
    {
        public string Token { get; set; } = string.Empty;
        public double Weight { get; set; }
        public string DoubleMetaphone { get; set; } = string.Empty;
    }
}
