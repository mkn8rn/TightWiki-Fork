using TightWiki.Contracts.DataModels;
using TightWiki.Localisation;

namespace TightWiki.Web.Bff.ViewModels.Admin
{
    public class ConfigurationViewModel : ViewModelBase
    {
        public List<Theme> Themes { get; set; } = new();
        public List<Role> Roles { get; set; } = new();
        public List<TimeZoneItem> TimeZones { get; set; } = new();
        public List<CountryItem> Countries { get; set; } = new();
        public List<LanguageItem> Languages { get; set; } = new();
        public List<ConfigurationNest> Nest { get; set; } = new();
    }
}

