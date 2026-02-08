using TightWiki.Web.Bff.ViewModels.Page;

namespace TightWiki.Web.Bff.Interfaces
{
    public interface ITagsBffService
    {
        BrowseViewModel GetBrowseViewModel(string givenCanonical);
    }
}
