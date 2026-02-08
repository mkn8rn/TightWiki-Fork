using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TightWiki.Web.Bff.ViewModels.Utility;
using TightWiki.Web.Filters;

namespace TightWiki.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class UtilityController : Controller
    {
        [AllowAnonymous]
        [ProducesView]
        [HttpGet("Notify")]
        public NotifyViewModel Notify(NotifyViewModel model)
            => model;

        [AllowAnonymous]
        [ProducesView]
        [HttpGet("ConfirmAction")]
        public ConfirmActionViewModel ConfirmAction(ConfirmActionViewModel model)
            => model;

        [AllowAnonymous]
        [ProducesView]
        [HttpPost("ConfirmAction")]
        [ActionName("ConfirmAction")]
        public ConfirmActionViewModel ConfirmActionPost(ConfirmActionViewModel model)
            => model;
    }
}
