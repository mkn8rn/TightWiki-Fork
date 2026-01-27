using TightWiki.Contracts.DataModels;

namespace TightWiki.Web.Bff.ViewModels.Admin
{
    public class DeletedPagesViewModel : ViewModelBase
    {
        public List<TightWiki.Contracts.DataModels.Page> Pages { get; set; } = new();
        public string SearchString { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}
