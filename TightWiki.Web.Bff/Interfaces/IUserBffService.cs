using TightWiki.Contracts.DTOs;

namespace TightWiki.Web.Bff.Interfaces
{
    /// <summary>
    /// BFF service for user operations.
    /// </summary>
    public interface IUserBffService
    {
        /// <summary>
        /// Gets a user profile by user ID.
        /// </summary>
        Task<UserDto?> GetUserByIdAsync(Guid userId);

        /// <summary>
        /// Gets a user's public profile by navigation.
        /// </summary>
        Task<UserDto?> GetPublicProfileAsync(string navigation);

        /// <summary>
        /// Gets the current user's permissions for a specific page.
        /// </summary>
        Task<UserPermissionsDto> GetUserPermissionsAsync(Guid? userId, string? pageNavigation);
    }

    /// <summary>
    /// User's effective permissions for display/UI logic.
    /// </summary>
    public class UserPermissionsDto
    {
        public bool IsAuthenticated { get; set; }
        public bool IsAdministrator { get; set; }
        public bool CanRead { get; set; }
        public bool CanEdit { get; set; }
        public bool CanCreate { get; set; }
        public bool CanDelete { get; set; }
        public bool CanModerate { get; set; }
    }
}
