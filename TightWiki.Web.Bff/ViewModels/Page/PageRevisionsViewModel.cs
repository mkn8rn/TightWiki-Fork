using TightWiki.Contracts.DataModels;

namespace TightWiki.Web.Bff.ViewModels.Page
{
    public class RevisionsViewModel : ViewModelBase
    {
        public List<PageRevision> Revisions { get; set; } = new();

        public int PaginationPageCount { get; set; }
    }
}
