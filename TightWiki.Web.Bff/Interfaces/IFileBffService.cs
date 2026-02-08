using Microsoft.AspNetCore.Mvc;
using TightWiki.Web.Bff.ViewModels.File;

namespace TightWiki.Web.Bff.Interfaces
{
    /// <summary>
    /// BFF service for file/attachment operations.
    /// </summary>
    public interface IFileBffService
    {
        IActionResult GetImage(FileImageRequest request);
        IActionResult GetPng(FileImageRequest request);
        IActionResult GetBinary(FileBinaryRequest request);
        PageFileRevisionsViewModel GetFileRevisionsViewModel(FileRevisionsRequest request);
        FileAttachmentViewModel GetPageAttachmentsViewModel(string givenPageNavigation);
        IActionResult UploadDragDrop(UploadDragDropRequest request);
        IActionResult ManualUpload(ManualUploadRequest request);
        IActionResult Detach(DetachFileRequest request);
        IActionResult AutoCompleteEmoji(string query);
        IActionResult GetEmoji(EmojiImageRequest request);
    }
}
