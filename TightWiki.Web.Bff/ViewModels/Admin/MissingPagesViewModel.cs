using TightWiki.Contracts.DataModels;

namespace TightWiki.Web.Bff.ViewModels.Admin
{
    public class MissingPagesViewModel : ViewModelBase
    {
        public List<NonexistentPage> Pages { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
