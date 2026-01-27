namespace TightWiki.Contracts.DataModels
{
    public class ProfileAvatar
    {
        public byte[]? Bytes { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }
}
