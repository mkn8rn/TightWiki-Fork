using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TightWiki.Web.Bff.Interfaces;
using TightWiki.Web.Bff.ViewModels.File;
using TightWiki.Web.Filters;

namespace TightWiki.Controllers
{
    [Route("File")]
    public class FileController(IFileBffService fileBff) : Controller
    {
        [HttpGet("Image/{givenPageNavigation}/{givenFileNavigation}/{fileRevision:int?}")]
        public IActionResult Image(FileImageRequest request)
            => fileBff.GetImage(request);

        [AllowAnonymous]
        [HttpGet("Png/{givenPageNavigation}/{givenFileNavigation}/{fileRevision:int?}")]
        public IActionResult Png(FileImageRequest request)
            => fileBff.GetPng(request);

        [AllowAnonymous]
        [HttpGet("Binary/{givenPageNavigation}/{givenFileNavigation}/{fileRevision:int?}")]
        public IActionResult Binary(FileBinaryRequest request)
            => fileBff.GetBinary(request);

        [Authorize]
        [ProducesView]
        [HttpGet("Revisions/{givenPageNavigation}/{givenFileNavigation}")]
        public PageFileRevisionsViewModel Revisions(FileRevisionsRequest request)
            => fileBff.GetFileRevisionsViewModel(request);

        [Authorize]
        [ProducesView]
        [HttpGet("PageAttachments/{givenPageNavigation}")]
        public FileAttachmentViewModel PageAttachments(string givenPageNavigation)
            => fileBff.GetPageAttachmentsViewModel(givenPageNavigation);

        [Authorize]
        [HttpPost("UploadDragDrop/{givenPageNavigation}")]
        public IActionResult UploadDragDrop(UploadDragDropRequest request)
            => fileBff.UploadDragDrop(request);

        [Authorize]
        [HttpPost("ManualUpload/{givenPageNavigation}")]
        public IActionResult ManualUpload(ManualUploadRequest request)
            => fileBff.ManualUpload(request);

        [HttpPost("Detach/{givenPageNavigation}/{givenFileNavigation}/{pageRevision}")]
        public IActionResult Detach(DetachFileRequest request)
            => fileBff.Detach(request);

        [Authorize]
        [HttpGet("AutoCompleteEmoji")]
        public IActionResult AutoCompleteEmoji([FromQuery] string? q = null)
            => fileBff.AutoCompleteEmoji(q ?? string.Empty);

        [AllowAnonymous]
        [HttpGet("Emoji/{givenEmojiNavigation}")]
        public IActionResult Emoji(EmojiImageRequest request)
            => fileBff.GetEmoji(request);
    }
}
