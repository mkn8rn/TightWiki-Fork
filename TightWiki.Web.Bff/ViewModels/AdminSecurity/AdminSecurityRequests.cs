using Microsoft.AspNetCore.Http;
using TightWiki.Web.Bff.ViewModels.Admin;
using TightWiki.Web.Bff.ViewModels.Utility;

namespace TightWiki.Web.Bff.ViewModels.AdminSecurity
{
    /// <summary>Parameters for the Role detail page.</summary>
    public class RoleRequest
    {
        public required string Navigation { get; init; }
        public int Page_Members { get; init; } = 1;
        public string? OrderBy_Members { get; init; }
        public string? OrderByDirection_Members { get; init; }
        public int Page_Permissions { get; init; } = 1;
        public string? OrderBy_Permission { get; init; }
        public string? OrderByDirection_Permissions { get; init; }
    }

    /// <summary>Parameters for the AccountRoles detail page.</summary>
    public class AccountRolesRequest
    {
        public required string Navigation { get; init; }
        public int Page_Memberships { get; init; } = 1;
        public string? OrderBy_Members { get; init; }
        public string? OrderByDirection_Memberships { get; init; }
        public int Page_Permissions { get; init; } = 1;
        public string? OrderBy_Permissions { get; init; }
        public string? OrderByDirection_Permissions { get; init; }
    }

    /// <summary>Parameters for deleting a role.</summary>
    public class DeleteRoleRequest
    {
        public int RoleId { get; init; }
        public required ConfirmActionViewModel Confirm { get; init; }
    }

    /// <summary>Parameters for saving an existing account.</summary>
    public class SaveAccountRequest
    {
        public required string Navigation { get; init; }
        public required AccountProfileViewModel Model { get; init; }
        public IFormFile? Avatar { get; init; }
    }

    /// <summary>Parameters for creating a new account.</summary>
    public class CreateAccountRequest
    {
        public required AccountProfileViewModel Model { get; init; }
        public IFormFile? Avatar { get; init; }
    }

    /// <summary>Parameters for deleting an account.</summary>
    public class DeleteAccountRequest
    {
        public required string Navigation { get; init; }
        public required ConfirmActionViewModel Confirm { get; init; }
    }

    /// <summary>Parameters for removing a role member.</summary>
    public class RemoveRoleMemberRequest
    {
        public int RoleId { get; init; }
        public Guid UserId { get; init; }
    }
}
