using TightWiki.Contracts.DataModels;

namespace TightWiki.Web.Bff.ViewModels.Admin
{
    public class PageCompilationStatisticsViewModel : ViewModelBase
    {
        public List<PageCompilationStatistics> Statistics { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
