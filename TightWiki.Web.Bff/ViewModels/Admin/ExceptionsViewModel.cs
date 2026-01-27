using TightWiki.Contracts.DataModels;

namespace TightWiki.Web.Bff.ViewModels.Admin
{
    public class ExceptionsViewModel : ViewModelBase
    {
        public List<WikiException> Exceptions { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
