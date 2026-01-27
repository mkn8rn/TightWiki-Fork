using TightWiki.Contracts.DataModels;

namespace TightWiki.Web.Bff.ViewModels.Page
{
    public class PageSearchViewModel : ViewModelBase
    {
        public List<TightWiki.Contracts.DataModels.Page> Pages { get; set; } = new();
        public string SearchString { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}
