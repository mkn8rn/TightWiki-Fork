namespace TightWiki.API.Models
{
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = [];
        public int PageNumber { get; set; }
        public int PageCount { get; set; }
    }

    public class PageSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public int Revision { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ModifiedByUserName { get; set; } = string.Empty;
    }

    public class PageDetailDto : PageSummaryDto
    {
        public string Body { get; set; } = string.Empty;
        public int MostCurrentRevision { get; set; }
        public string ChangeSummary { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class PageRevisionDto
    {
        public int PageId { get; set; }
        public int Revision { get; set; }
        public int HighestRevision { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string ChangeSummary { get; set; } = string.Empty;
        public string ModifiedByUserName { get; set; } = string.Empty;
        public DateTime ModifiedDate { get; set; }
    }

    public class NamespaceDto
    {
        public string Namespace { get; set; } = string.Empty;
        public int CountOfPages { get; set; }
    }

    public class PageCommentDto
    {
        public int Id { get; set; }
        public string Body { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class TagDto
    {
        public string Tag { get; set; } = string.Empty;
        public int PageCount { get; set; }
    }

    public class ProfileDto
    {
        public string AccountName { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Country { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string? Biography { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }

    public class FileAttachmentDto
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public string FileNavigation { get; set; } = string.Empty;
        public string PageNavigation { get; set; } = string.Empty;
        public int FileRevision { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
    }

    public class SearchResultDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public decimal Match { get; set; }
    }

    public class StatisticsDto
    {
        public int Pages { get; set; }
        public int PageRevisions { get; set; }
        public int PageAttachments { get; set; }
        public int PageTags { get; set; }
        public int Users { get; set; }
        public int Namespaces { get; set; }
    }

    public class DatabaseExportDto
    {
        public DateTime ExportedAtUtc { get; set; }
        public StatisticsDto Statistics { get; set; } = new();
        public List<PageDetailDto> Pages { get; set; } = [];
        public List<NamespaceDto> Namespaces { get; set; } = [];
        public List<ProfileDto> Profiles { get; set; } = [];
    }
}
