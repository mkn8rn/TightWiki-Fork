using TightWiki.Contracts.DataModels;

namespace TightWiki.Web.Bff.ViewModels.AdminSecurity
{
    public class RolesViewModel : ViewModelBase
    {
        public List<Role> Roles { get; set; } = new();
    }
}
