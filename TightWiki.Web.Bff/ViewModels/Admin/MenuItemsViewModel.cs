using TightWiki.Contracts.DataModels;

namespace TightWiki.Web.Bff.ViewModels.Admin
{
    public class MenuItemsViewModel : ViewModelBase
    {
        public List<MenuItem> Items { get; set; } = new();
    }
}
