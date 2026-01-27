using System.Globalization;

namespace TightWiki.Web.Bff.ViewModels.Page
{
    public class PageLocalizationViewModel : ViewModelBase
    {
        public List<CultureInfo> Languages { get; set; } = new();
    }
}
