using TightWiki.Contracts.DataModels;

namespace TightWiki.Web.Bff.ViewModels.Admin
{
    public class PageRevisionsViewModel : ViewModelBase
    {
        public List<PageRevision> Revisions { get; set; } = new();

        public int PaginationPageCount { get; set; }
    }
}
