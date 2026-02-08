using Microsoft.AspNetCore.Mvc;
using TightWiki.Web.Bff.ViewModels.Page;

namespace TightWiki.Web.Bff.Interfaces
{
    public interface IPageBffService
    {
        // Display
        DisplayPageResult GetDisplayResult(PageDisplayRequest request);

        // Search
        IActionResult AutoCompletePage(string? query);
        PageSearchViewModel GetSearchViewModel(PageSearchRequest request);

        // Localization
        PageLocalizationViewModel GetLocalizationViewModel();
        IActionResult SetLocalization(SetLocalizationRequest request);

        // Comments
        PageCommentsViewModel GetCommentsViewModel(PageCommentsRequest request);
        PageCommentsViewModel PostComment(PostCommentRequest request);

        // Refresh
        IActionResult RefreshPage(string givenCanonical);

        // Compare
        PageCompareViewModel GetCompareViewModel(PageCompareRequest request);

        // Revisions
        RevisionsViewModel GetRevisionsViewModel(PageRevisionsRequest request);

        // Delete
        IActionResult DeletePage(PageDeleteRequest request);
        PageDeleteViewModel GetDeleteViewModel(string givenCanonical);

        // Revert
        IActionResult RevertPage(PageRevertRequest request);
        PageRevertViewModel GetRevertViewModel(string givenCanonical, int pageRevision);

        // Edit
        PageEditViewModel GetEditViewModel(string givenCanonical, string? pageName);
        IActionResult SavePage(PageEditViewModel model);
        IActionResult GetTemplateBody(int id);

        // Export
        IActionResult Export(PageExportRequest request);
    }
}
