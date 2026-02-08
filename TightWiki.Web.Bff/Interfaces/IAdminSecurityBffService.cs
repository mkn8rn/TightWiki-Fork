using Microsoft.AspNetCore.Mvc;
using TightWiki.Contracts.Requests;
using TightWiki.Web.Bff.ViewModels.Admin;
using TightWiki.Web.Bff.ViewModels.AdminSecurity;

namespace TightWiki.Web.Bff.Interfaces
{
    public interface IAdminSecurityBffService
    {
        // Roles
        IActionResult DeleteRole(DeleteRoleRequest request);
        IActionResult AddAccountMembership(AddAccountMembershipRequest request);
        IActionResult AddRoleMember(AddRoleMemberRequest request);
        IActionResult RemoveRoleMember(RemoveRoleMemberRequest request);
        IActionResult RemoveRolePermission(int id);
        IActionResult AddRolePermission(AddRolePermissionRequest request);
        IActionResult AddRole(AddRoleViewModel model);
        AddRoleViewModel GetAddRoleViewModel();
        RoleViewModel GetRoleViewModel(RoleRequest request);
        RolesViewModel GetRolesViewModel(PagedRequest request);

        // Account Roles
        IActionResult AddAccountPermission(AddAccountPermissionRequest request);
        IActionResult RemoveAccountPermission(int id);
        AccountRolesViewModel GetAccountRolesViewModel(AccountRolesRequest request);

        // Accounts
        AccountProfileViewModel GetAccountViewModel(string navigation);
        AccountProfileViewModel GetAddAccountViewModel();
        AccountsViewModel GetAccountsViewModel(SearchPagedRequest request);
        IActionResult SaveAccount(SaveAccountRequest request);
        IActionResult CreateAccount(CreateAccountRequest request);
        IActionResult DeleteAccount(DeleteAccountRequest request);

        // AutoComplete
        IActionResult AutoCompleteRole(string? q);
        IActionResult AutoCompleteAccount(string? q);
        IActionResult AutoCompletePage(string? q, bool? showCatchAll);
        IActionResult AutoCompleteNamespace(string? q, bool? showCatchAll);
    }
}
