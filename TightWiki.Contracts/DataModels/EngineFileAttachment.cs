namespace TightWiki.Contracts.DataModels
{
    /// <summary>
    /// File attachment metadata for display in wiki pages.
    /// </summary>
    public class EngineFileAttachment
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FileNavigation { get; set; } = string.Empty;
        public string PageNavigation { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public int FileRevision { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public string CreatedByNavigation { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
        public int PaginationPageSize { get; set; }

        public string FriendlySize => FormatFileSize(Size);

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = ["B", "KB", "MB", "GB", "TB"];
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }
}
