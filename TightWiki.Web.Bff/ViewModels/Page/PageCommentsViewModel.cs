using TightWiki.Contracts.DataModels;

namespace TightWiki.Web.Bff.ViewModels.Page
{
    public class PageCommentsViewModel : ViewModelBase
    {
        public List<PageComment> Comments { get; set; } = new();
        public string Comment { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}
