using Microsoft.AspNetCore.Http;
using TightWiki.Web.Bff.ViewModels.Utility;

namespace TightWiki.Web.Bff.ViewModels.Profile
{
    /// <summary>Parameters for avatar retrieval.</summary>
    public class GetAvatarRequest
    {
        public required string UserAccountName { get; init; }
        public string? Scale { get; init; }
        public string? Max { get; init; }
        public string? Exact { get; init; }
    }

    /// <summary>Parameters for saving the user's own profile.</summary>
    public class SaveMyProfileRequest
    {
        public required AccountProfileViewModel Model { get; init; }
        public IFormFile? Avatar { get; init; }
    }

    /// <summary>Parameters for deleting the user's own account.</summary>
    public class DeleteMyAccountRequest
    {
        public required ConfirmActionViewModel Confirm { get; init; }
    }

    public class ImageResult
    {
        public required byte[] Bytes { get; init; }
        public required string ContentType { get; init; }
    }
}
