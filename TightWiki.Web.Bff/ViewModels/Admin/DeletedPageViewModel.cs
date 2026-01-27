using TightWiki.Contracts.DataModels;

namespace TightWiki.Web.Bff.ViewModels.Page
{
    public class DeletedPageViewModel : ViewModelBase
    {
        public int PageId { get; set; }
        public string Body { get; set; } = string.Empty;
        public string DeletedByUserName { get; set; } = string.Empty;
        public DateTime DeletedDate { get; set; }
        public List<PageComment> Comments { get; set; } = new();
    }
}
