using TightWiki.Contracts.DataModels;
using static TightWiki.Contracts.Constants;

namespace BLL.Services.Users
{
    /// <summary>
    /// Service interface for user and profile operations.
    /// </summary>
    public interface IUsersService
    {
        #region Admin Password Management

        /// <summary>
        /// Gets the current admin password status.
        /// </summary>
        AdminPasswordChangeState GetAdminPasswordStatus();

        /// <summary>
        /// Clears the admin password status (marks as needing to be set).
        /// </summary>
        void SetAdminPasswordClear();

        /// <summary>
        /// Marks the admin password as having been changed from default.
        /// </summary>
        void SetAdminPasswordIsChanged();

        /// <summary>
        /// Marks the admin password as being set to the default.
        /// </summary>
        void SetAdminPasswordIsDefault();

        #endregion

        #region Profile Management

        /// <summary>
        /// Creates a new user profile.
        /// </summary>
        void CreateProfile(Guid userId, string accountName);

        /// <summary>
        /// Gets a user account ID by navigation path.
        /// </summary>
        Guid? GetUserAccountIdByNavigation(string navigation);

        /// <summary>
        /// Sets the user ID for an existing profile.
        /// </summary>
        void SetProfileUserId(string navigation, Guid userId);

        /// <summary>
        /// Gets account profile by user ID.
        /// </summary>
        AccountProfile GetAccountProfileByUserId(Guid userId);

        /// <summary>
        /// Gets basic profile information by user ID.
        /// </summary>
        AccountProfile? GetBasicProfileByUserId(Guid userId);

        /// <summary>
        /// Tries to get basic profile information by user ID.
        /// </summary>
        bool TryGetBasicProfileByUserId(Guid userId, out AccountProfile? profile);

        /// <summary>
        /// Gets account profile by navigation path.
        /// </summary>
        AccountProfile GetAccountProfileByNavigation(string? navigation);

        /// <summary>
        /// Checks if a profile account exists.
        /// </summary>
        bool DoesProfileAccountExist(string navigation);

        /// <summary>
        /// Checks if an email address already exists.
        /// </summary>
        bool DoesEmailAddressExist(string? emailAddress);

        /// <summary>
        /// Updates a user profile.
        /// </summary>
        void UpdateProfile(AccountProfile profile);

        /// <summary>
        /// Updates a user's avatar image.
        /// </summary>
        void UpdateProfileAvatar(Guid userId, byte[] imageData, string contentType);

        /// <summary>
        /// Gets a user's avatar by navigation.
        /// </summary>
        ProfileAvatar? GetProfileAvatarByNavigation(string navigation);

        /// <summary>
        /// Anonymizes a user profile.
        /// </summary>
        void AnonymizeProfile(Guid userId);

        /// <summary>
        /// Gets all users.
        /// </summary>
        List<AccountProfile> GetAllUsers();

        /// <summary>
        /// Gets users with pagination.
        /// </summary>
        List<AccountProfile> GetAllUsersPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, string? searchToken = null);

        /// <summary>
        /// Gets public profiles with pagination.
        /// </summary>
        List<AccountProfile> GetAllPublicProfilesPaged(int pageNumber, int? pageSize = null, string? searchToken = null);

        #endregion

        #region Role Management

        /// <summary>
        /// Adds a user to a role by role name.
        /// </summary>
        AddRoleMemberResult? AddRoleMemberByName(Guid userId, string roleName);

        /// <summary>
        /// Adds a user to a role.
        /// </summary>
        AddRoleMemberResult? AddRoleMember(Guid userId, int roleId);

        /// <summary>
        /// Removes a user from a role.
        /// </summary>
        void RemoveRoleMember(int roleId, Guid userId);

        /// <summary>
        /// Gets all roles.
        /// </summary>
        List<Role> GetAllRoles(string? orderBy = null, string? orderByDirection = null);

        /// <summary>
        /// Gets a role by name.
        /// </summary>
        Role GetRoleByName(string name);

        /// <summary>
        /// Checks if a role exists.
        /// </summary>
        bool DoesRoleExist(string name);

        /// <summary>
        /// Creates a new role.
        /// </summary>
        bool InsertRole(string name, string? description);

        /// <summary>
        /// Deletes a role.
        /// </summary>
        void DeleteRole(int roleId);

        /// <summary>
        /// Gets role members with pagination.
        /// </summary>
        List<AccountProfile> GetRoleMembersPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);

        /// <summary>
        /// Checks if an account is a member of a role.
        /// </summary>
        bool IsAccountAMemberOfRole(Guid userId, int roleId);

        /// <summary>
        /// Checks if user is a member of the Administrators role.
        /// </summary>
        bool IsUserMemberOfAdministrators(Guid userId);

        /// <summary>
        /// Adds a membership for a user to a role.
        /// </summary>
        AddAccountMembershipResult? AddAccountMembership(Guid userId, int roleId);

        /// <summary>
        /// Gets account role memberships with pagination.
        /// </summary>
        List<AccountRoleMembership> GetAccountRoleMembershipPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);

        /// <summary>
        /// Autocomplete for roles.
        /// </summary>
        IEnumerable<Role> AutoCompleteRole(string? searchText);

        /// <summary>
        /// Autocomplete for accounts.
        /// </summary>
        IEnumerable<AccountProfile> AutoCompleteAccount(string? searchText);

        #endregion

        #region Permission Management

        /// <summary>
        /// Gets all permissions.
        /// </summary>
        List<Permission> GetAllPermissions();

        /// <summary>
        /// Gets all permission dispositions.
        /// </summary>
        List<PermissionDisposition> GetAllPermissionDispositions();

        /// <summary>
        /// Gets apparent account permissions.
        /// </summary>
        List<ApparentPermission> GetApparentAccountPermissions(Guid userId);

        /// <summary>
        /// Gets apparent role permissions.
        /// </summary>
        List<ApparentPermission> GetApparentRolePermissions(string roleName);

        /// <summary>
        /// Gets role permissions with pagination.
        /// </summary>
        List<RolePermission> GetRolePermissionsPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);

        /// <summary>
        /// Gets account permissions with pagination.
        /// </summary>
        List<AccountPermission> GetAccountPermissionsPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);

        /// <summary>
        /// Checks if an account permission is defined.
        /// </summary>
        bool IsAccountPermissionDefined(Guid userId, int permissionId, string permissionDispositionId, string? ns, string? pageId);

        /// <summary>
        /// Checks if a role permission is defined.
        /// </summary>
        bool IsRolePermissionDefined(int roleId, int permissionId, string permissionDispositionId, string? ns, string? pageId);

        /// <summary>
        /// Inserts an account permission.
        /// </summary>
        InsertAccountPermissionResult? InsertAccountPermission(Guid userId, int permissionId, string permissionDisposition, string? ns, string? pageId);

        /// <summary>
        /// Inserts a role permission.
        /// </summary>
        InsertRolePermissionResult? InsertRolePermission(int roleId, int permissionId, string permissionDisposition, string? ns, string? pageId);

        /// <summary>
        /// Removes an account permission.
        /// </summary>
        void RemoveAccountPermission(int id);

        /// <summary>
        /// Removes a role permission.
        /// </summary>
        void RemoveRolePermission(int id);

        #endregion
    }
}
