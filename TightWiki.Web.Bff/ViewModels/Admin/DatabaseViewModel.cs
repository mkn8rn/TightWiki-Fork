using TightWiki.Contracts.DataModels;

namespace TightWiki.Web.Bff.ViewModels.Admin
{
    public class DatabaseViewModel : ViewModelBase
    {
        public List<DatabaseInfo> Info { get; set; } = new();
    }
}
