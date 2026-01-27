namespace TightWiki.Contracts.DataModels
{
    /// <summary>
    /// Tag cloud item for display in tag/search clouds.
    /// </summary>
    public class TagCloudItem
    {
        public string Name = "";
        public string HTML = "";
        public int Rank = 0;

        public TagCloudItem(string name, int rank, string html)
        {
            Name = name;
            HTML = html;
            Rank = rank;
        }

        public static int CompareItem(TagCloudItem x, TagCloudItem y)
        {
            return string.Compare(x.Name, y.Name);
        }
    }
}
