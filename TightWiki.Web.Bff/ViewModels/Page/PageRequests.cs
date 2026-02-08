using TightWiki.Web.Bff.ViewModels.Admin;

namespace TightWiki.Web.Bff.ViewModels.Page
{
    /// <summary>Parameters for displaying a page.</summary>
    public class PageDisplayRequest
    {
        public string GivenCanonical { get; init; } = "home";
        public int? PageRevision { get; init; }
    }

    /// <summary>Parameters for search.</summary>
    public class PageSearchRequest
    {
        public string SearchString { get; init; } = string.Empty;
        public int Page { get; init; } = 1;
    }

    /// <summary>Parameters for page comments.</summary>
    public class PageCommentsRequest
    {
        public required string GivenCanonical { get; init; }
        public string? Delete { get; init; }
        public int Page { get; init; } = 1;
    }

    /// <summary>Parameters for posting a comment.</summary>
    public class PostCommentRequest
    {
        public required string GivenCanonical { get; init; }
        public required PageCommentsViewModel Model { get; init; }
        public int Page { get; init; } = 1;
    }

    /// <summary>Parameters for page compare.</summary>
    public class PageCompareRequest
    {
        public required string GivenCanonical { get; init; }
        public int PageRevision { get; init; }
    }

    /// <summary>Parameters for page revisions listing.</summary>
    public class PageRevisionsRequest
    {
        public required string GivenCanonical { get; init; }
        public int Page { get; init; } = 1;
        public string? OrderBy { get; init; }
        public string? OrderByDirection { get; init; }
    }

    /// <summary>Parameters for page deletion (POST).</summary>
    public class PageDeleteRequest
    {
        public required string GivenCanonical { get; init; }
        public required PageDeleteViewModel Model { get; init; }
        public bool IsActionConfirmed { get; init; }
    }

    /// <summary>Parameters for page revert (POST).</summary>
    public class PageRevertRequest
    {
        public required string GivenCanonical { get; init; }
        public int PageRevision { get; init; }
        public required PageRevertViewModel Model { get; init; }
        public bool IsActionConfirmed { get; init; }
    }

    /// <summary>Parameters for saving a page.</summary>
    public class SavePageRequest
    {
        public required PageEditViewModel Model { get; init; }
        public Guid UserId { get; init; }
    }

    /// <summary>Result of a save operation.</summary>
    public class SavePageResult
    {
        public bool Success { get; set; }
        public int PageId { get; set; }
        public string Navigation { get; set; } = string.Empty;
        public string? OriginalNavigation { get; set; }
        public Dictionary<string, string> ModelErrors { get; } = new();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public bool IsNew { get; set; }
    }

    /// <summary>Parameters for page export.</summary>
    public class PageExportRequest
    {
        public required string GivenCanonical { get; init; }
    }

    /// <summary>Parameters for setting localization.</summary>
    public class SetLocalizationRequest
    {
        public required string Culture { get; init; }
        public required string ReturnUrl { get; init; }
    }
}
