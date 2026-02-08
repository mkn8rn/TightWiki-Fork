using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TightWiki.Contracts.Requests;
using TightWiki.Web.Bff.Interfaces;
using TightWiki.Web.Bff.ViewModels.Admin;
using TightWiki.Web.Bff.ViewModels.AdminSecurity;
using TightWiki.Web.Filters;

namespace TightWiki.Controllers
{
    [Authorize]
    [RequireWikiAdmin]
    [Route("[controller]")]
    public class AdminSecurityController(IAdminSecurityBffService secBff) : Controller
    {
        #region Roles

        [HttpPost("DeleteRole/{roleId:int}")]
        public IActionResult DeleteRole(DeleteRoleRequest request)
            => secBff.DeleteRole(request);

        [HttpPost("AddAccountMembership")]
        public IActionResult AddAccountMembership([FromBody] AddAccountMembershipRequest request)
            => secBff.AddAccountMembership(request);

        [HttpPost("AddRoleMember")]
        public IActionResult AddRoleMember([FromBody] AddRoleMemberRequest request)
            => secBff.AddRoleMember(request);

        [HttpPost("RemoveRoleMember/{roleId:int}/{userId:Guid}")]
        public IActionResult RemoveRoleMember(RemoveRoleMemberRequest request)
            => secBff.RemoveRoleMember(request);

        [HttpPost("RemoveRolePermission/{id:int}")]
        public IActionResult RemoveRolePermission(int id)
            => secBff.RemoveRolePermission(id);

        [HttpPost("AddRolePermission")]
        public IActionResult AddRolePermission([FromBody] AddRolePermissionRequest request)
            => secBff.AddRolePermission(request);

        [ProducesView]
        [HttpGet("AddRole")]
        public AddRoleViewModel AddRole()
            => secBff.GetAddRoleViewModel();

        [HttpPost("AddRole")]
        public IActionResult AddRole(AddRoleViewModel model)
            => secBff.AddRole(model);

        [ProducesView]
        [HttpGet("Role/{navigation}")]
        public RoleViewModel Role(RoleRequest request)
            => secBff.GetRoleViewModel(request);

        [ProducesView]
        [HttpGet("Roles")]
        public RolesViewModel Roles(PagedRequest request)
            => secBff.GetRolesViewModel(request);

        #endregion

        #region Account Roles

        [HttpPost("AddAccountPermission")]
        public IActionResult AddAccountPermission([FromBody] AddAccountPermissionRequest request)
            => secBff.AddAccountPermission(request);

        [HttpPost("RemoveAccountPermission/{id:int}")]
        public IActionResult RemoveAccountPermission(int id)
            => secBff.RemoveAccountPermission(id);

        [ProducesView]
        [HttpGet("AccountRoles/{navigation}")]
        public AccountRolesViewModel AccountRoles(AccountRolesRequest request)
            => secBff.GetAccountRolesViewModel(request);

        #endregion

        #region Accounts

        [ProducesView]
        [HttpGet("Account/{navigation}")]
        public AccountProfileViewModel Account(string navigation)
            => secBff.GetAccountViewModel(navigation);

        [HttpPost("Account/{navigation}")]
        public IActionResult Account(SaveAccountRequest request)
            => secBff.SaveAccount(request);

        [ProducesView]
        [HttpGet("AddAccount")]
        public AccountProfileViewModel AddAccount()
            => secBff.GetAddAccountViewModel();

        [HttpPost("AddAccount")]
        public IActionResult AddAccount(CreateAccountRequest request)
            => secBff.CreateAccount(request);

        [ProducesView]
        [HttpGet("Accounts")]
        public AccountsViewModel Accounts(SearchPagedRequest request)
            => secBff.GetAccountsViewModel(request);

        [HttpPost("DeleteAccount/{navigation}")]
        public IActionResult DeleteAccount(DeleteAccountRequest request)
            => secBff.DeleteAccount(request);

        #endregion

        #region AutoComplete

        [HttpGet("AutoCompleteRole")]
        public IActionResult AutoCompleteRole([FromQuery] string? q = null)
            => secBff.AutoCompleteRole(q);

        [HttpGet("AutoCompleteAccount")]
        public IActionResult AutoCompleteAccount([FromQuery] string? q = null)
            => secBff.AutoCompleteAccount(q);

        [HttpGet("AutoCompletePage")]
        public IActionResult AutoCompletePage([FromQuery] string? q = null, [FromQuery] bool? showCatchAll = false)
            => secBff.AutoCompletePage(q, showCatchAll);

        [HttpGet("AutoCompleteNamespace")]
        public IActionResult AutoCompleteNamespace([FromQuery] string? q = null, [FromQuery] bool? showCatchAll = false)
            => secBff.AutoCompleteNamespace(q, showCatchAll);

        #endregion
    }
}
