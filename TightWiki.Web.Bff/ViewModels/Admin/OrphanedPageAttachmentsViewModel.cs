using TightWiki.Contracts.DataModels;

namespace TightWiki.Web.Bff.ViewModels.Admin
{
    public class OrphanedPageAttachmentsViewModel : ViewModelBase
    {
        public List<OrphanedPageAttachment> Files { get; set; } = new();
        public int PaginationPageCount { get; set; }
    }
}
