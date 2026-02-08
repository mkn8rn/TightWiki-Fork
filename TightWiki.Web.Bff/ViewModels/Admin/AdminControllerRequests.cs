using Microsoft.AspNetCore.Http;
using TightWiki.Web.Bff.ViewModels.Utility;

namespace TightWiki.Web.Bff.ViewModels.Admin
{
    /// <summary>Common query parameters for paged, sortable lists.</summary>
    public class PagedRequest
    {
        public int Page { get; init; } = 1;
        public string? OrderBy { get; init; }
        public string? OrderByDirection { get; init; }
    }

    /// <summary>Extends <see cref="PagedRequest"/> with a search term.</summary>
    public class SearchPagedRequest : PagedRequest
    {
        public string SearchString { get; init; } = string.Empty;
    }

    /// <summary>Parameters for the Moderate page.</summary>
    public class ModerateRequest : PagedRequest
    {
        public string? Instruction { get; init; }
    }

    /// <summary>Parameters for the Namespace detail page.</summary>
    public class NamespaceRequest : PagedRequest
    {
        public string? NamespaceName { get; init; }
    }

    /// <summary>Parameters for database admin actions.</summary>
    public class DatabaseActionRequest
    {
        public required string DatabaseAction { get; init; }
        public required string Database { get; init; }
        public required ConfirmActionViewModel Confirm { get; init; }
    }

    /// <summary>Wraps a <see cref="ConfirmActionViewModel"/> with a page identifier.</summary>
    public class ConfirmPageRequest
    {
        public int PageId { get; init; }
        public required ConfirmActionViewModel Confirm { get; init; }
    }

    /// <summary>Wraps a <see cref="ConfirmActionViewModel"/> with a page identifier and revision.</summary>
    public class ConfirmPageRevisionRequest
    {
        public int PageId { get; init; }
        public int Revision { get; init; }
        public required ConfirmActionViewModel Confirm { get; init; }
    }

    /// <summary>Parameters for reverting or deleting a page revision by canonical navigation.</summary>
    public class ConfirmCanonicalRevisionRequest
    {
        public required string GivenCanonical { get; init; }
        public int Revision { get; init; }
        public required ConfirmActionViewModel Confirm { get; init; }
    }

    /// <summary>Parameters for saving a menu item.</summary>
    public class SaveMenuItemRequest
    {
        public int? Id { get; init; }
        public required MenuItemViewModel Model { get; init; }
    }

    /// <summary>Wraps a <see cref="ConfirmActionViewModel"/> with a menu-item identifier.</summary>
    public class DeleteMenuItemRequest
    {
        public int Id { get; init; }
        public required ConfirmActionViewModel Confirm { get; init; }
    }

    /// <summary>Parameters for saving an emoji (existing), including file upload data.</summary>
    public class SaveEmojiUploadRequest
    {
        public required EmojiViewModel Model { get; init; }
        public IFormFile? ImageFile { get; init; }
    }

    /// <summary>Parameters for creating a new emoji, including file upload data.</summary>
    public class CreateEmojiUploadRequest
    {
        public required AddEmojiViewModel Model { get; init; }
        public IFormFile? ImageFile { get; init; }
    }

    /// <summary>Wraps a <see cref="ConfirmActionViewModel"/> with a name.</summary>
    public class DeleteEmojiRequest
    {
        public required string Name { get; init; }
        public required ConfirmActionViewModel Confirm { get; init; }
    }

    /// <summary>Parameters for orphaned-attachment purge by specific file.</summary>
    public class PurgeOrphanedAttachmentRequest
    {
        public int PageFileId { get; init; }
        public int Revision { get; init; }
        public required ConfirmActionViewModel Confirm { get; init; }
    }

    /// <summary>Parameters for LDAP test.</summary>
    public class LdapTestRequest
    {
        public required string Username { get; init; }
        public required string Password { get; init; }
    }
}
