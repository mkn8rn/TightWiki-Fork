using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using TightWiki.Web.Bff.Interfaces;
using TightWiki.Web.Bff.ViewModels.Page;
using TightWiki.Web.Filters;

namespace TightWiki.Controllers
{
    [Route("")]
    public class PageController(IPageBffService pageBff) : Controller
    {
        [AllowAnonymous]
        [Route("/robots.txt")]
        public ContentResult RobotsTxt()
            => Content(new StringBuilder().AppendLine("User-agent: *").AppendLine("Allow: /").ToString(), "text/plain", Encoding.UTF8);

        [Authorize]
        [Route("/ping")]
        public JsonResult Ping()
            => Json(new { now = DateTime.UtcNow });

        #region Display

        [ProducesView]
        [HttpGet]
        public PageDisplayViewModel Display()
            => pageBff.GetDisplayResult(new PageDisplayRequest()).Model;

        [ProducesView]
        [HttpGet("{givenCanonical}/{pageRevision:int?}")]
        public PageDisplayViewModel Display(PageDisplayRequest request)
            => pageBff.GetDisplayResult(request).Model;

        #endregion

        #region Search

        [Authorize]
        [HttpGet("Page/AutoCompletePage")]
        public IActionResult AutoCompletePage([FromQuery] string? q = null)
            => pageBff.AutoCompletePage(q);

        [AllowAnonymous]
        [ProducesView]
        [HttpGet("Page/Search")]
        public PageSearchViewModel Search(PageSearchRequest request)
            => pageBff.GetSearchViewModel(request);

        [AllowAnonymous]
        [ProducesView]
        [HttpPost("Page/Search")]
        public PageSearchViewModel SearchPost(PageSearchRequest request)
            => pageBff.GetSearchViewModel(request);

        #endregion

        #region Localization

        [AllowAnonymous]
        [ProducesView]
        [HttpGet("Page/Localization")]
        public PageLocalizationViewModel Localization()
            => pageBff.GetLocalizationViewModel();

        [AllowAnonymous]
        [HttpGet("Page/SetLocalization")]
        public IActionResult SetLocalization(SetLocalizationRequest request)
            => pageBff.SetLocalization(request);

        #endregion

        #region Comments

        [AllowAnonymous]
        [ProducesView]
        [HttpGet("{givenCanonical}/Comments")]
        public PageCommentsViewModel Comments(PageCommentsRequest request)
            => pageBff.GetCommentsViewModel(request);

        [Authorize]
        [ProducesView]
        [HttpPost("{givenCanonical}/Comments")]
        public PageCommentsViewModel Comments(PostCommentRequest request)
            => pageBff.PostComment(request);

        #endregion

        #region Refresh

        [Authorize]
        [HttpGet("{givenCanonical}/Refresh")]
        public IActionResult Refresh(string givenCanonical)
            => pageBff.RefreshPage(givenCanonical);

        #endregion

        #region Compare

        [Authorize]
        [ProducesView]
        [HttpGet("{givenCanonical}/Compare/{pageRevision:int}")]
        public PageCompareViewModel Compare(PageCompareRequest request)
            => pageBff.GetCompareViewModel(request);

        #endregion

        #region Revisions

        [Authorize]
        [ProducesView]
        [HttpGet("{givenCanonical}/Revisions")]
        public RevisionsViewModel Revisions(PageRevisionsRequest request)
            => pageBff.GetRevisionsViewModel(request);

        #endregion

        #region Delete

        [Authorize]
        [HttpPost("{givenCanonical}/Delete")]
        public IActionResult Delete(PageDeleteRequest request)
            => pageBff.DeletePage(request);

        [Authorize]
        [ProducesView]
        [HttpGet("{givenCanonical}/Delete")]
        public PageDeleteViewModel Delete(string givenCanonical)
            => pageBff.GetDeleteViewModel(givenCanonical);

        #endregion

        #region Revert

        [Authorize]
        [HttpPost("{givenCanonical}/Revert/{pageRevision:int}")]
        public IActionResult Revert(PageRevertRequest request)
            => pageBff.RevertPage(request);

        [Authorize]
        [ProducesView]
        [HttpGet("{givenCanonical}/Revert/{pageRevision:int}")]
        public PageRevertViewModel Revert(string givenCanonical, int pageRevision)
            => pageBff.GetRevertViewModel(givenCanonical, pageRevision);

        #endregion

        #region Edit

        [Authorize]
        [ProducesView]
        [HttpGet("{givenCanonical}/Create")]
        [HttpGet("{givenCanonical}/Edit")]
        [HttpGet("Page/Create")]
        public PageEditViewModel Edit(string givenCanonical, [FromQuery] string? Name = null)
            => pageBff.GetEditViewModel(givenCanonical, Name);

        [Authorize]
        [HttpPost("{givenCanonical}/Create")]
        [HttpPost("{givenCanonical}/Edit")]
        [HttpPost("Page/Create")]
        public IActionResult Edit(PageEditViewModel model)
            => pageBff.SavePage(model);

        [Authorize]
        [HttpGet("Page/Template/{id:int}")]
        public IActionResult Template(int id)
            => pageBff.GetTemplateBody(id);

        #endregion

        #region File (route aliases)

        [HttpGet("Page/Image/{givenPageNavigation}/{givenFileNavigation}/{pageRevision:int?}")]
        public IActionResult Image(string givenPageNavigation, string givenFileNavigation, int? pageRevision = null)
            => RedirectToAction("Image", "File", new { givenPageNavigation, givenFileNavigation, fileRevision = pageRevision });

        [AllowAnonymous]
        [HttpGet("Page/Png/{givenPageNavigation}/{givenFileNavigation}/{pageRevision:int?}")]
        public IActionResult Png(string givenPageNavigation, string givenFileNavigation, int? pageRevision = null)
            => RedirectToAction("Png", "File", new { givenPageNavigation, givenFileNavigation, fileRevision = pageRevision });

        [AllowAnonymous]
        [HttpGet("Page/Binary/{givenPageNavigation}/{givenFileNavigation}/{pageRevision:int?}")]
        public IActionResult Binary(string givenPageNavigation, string givenFileNavigation, int? pageRevision = null)
            => RedirectToAction("Binary", "File", new { givenPageNavigation, givenFileNavigation, fileRevision = pageRevision });

        #endregion

        #region Export

        [Authorize]
        [HttpGet("{givenCanonical}/Export")]
        public IActionResult Export(PageExportRequest request)
            => pageBff.Export(request);

        #endregion
    }
}
