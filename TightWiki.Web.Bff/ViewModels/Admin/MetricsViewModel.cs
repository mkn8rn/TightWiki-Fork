using TightWiki.Contracts.DataModels;

namespace TightWiki.Web.Bff.ViewModels.Admin
{
    public class MetricsViewModel : ViewModelBase
    {
        public WikiDatabaseStatistics Metrics { get; set; } = new();
        public string ApplicationVersion { get; set; } = string.Empty;
    }
}
