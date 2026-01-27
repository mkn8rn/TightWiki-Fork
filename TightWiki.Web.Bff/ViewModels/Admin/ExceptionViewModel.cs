using TightWiki.Contracts.DataModels;

namespace TightWiki.Web.Bff.ViewModels.Admin
{
    public class ExceptionViewModel : ViewModelBase
    {
        public WikiException Exception { get; set; } = new();
    }
}
