using TightWiki.Contracts.DataModels;

namespace TightWiki.Web.Bff.ViewModels.Admin
{
    public class PageModerateViewModel : ViewModelBase
    {
        public List<string> Instructions { get; set; } = new();
        public List<TightWiki.Contracts.DataModels.Page> Pages { get; set; } = new();
        public string Instruction { get; set; } = string.Empty;
        public int PaginationPageCount { get; set; }
    }
}
