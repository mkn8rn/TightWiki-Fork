using TightWiki.Contracts.DataModels;

namespace TightWiki.Web.Bff.ViewModels.Admin
{
    public class MenuItemViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public int Ordinal { get; set; }

        public MenuItem ToDataModel()
        {
            return new MenuItem
            {
                Name = Name,
                Id = Id,
                Link = Link,
                Ordinal = Ordinal
            };
        }

        public static MenuItemViewModel FromDataModel(MenuItem menuItem)
        {
            return new MenuItemViewModel
            {
                Id = menuItem.Id,
                Name = menuItem.Name,
                Link = menuItem.Link,
                Ordinal = menuItem.Ordinal
            };
        }
    }
}
