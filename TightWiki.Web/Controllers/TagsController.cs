using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TightWiki.Web.Bff.Interfaces;
using TightWiki.Web.Bff.ViewModels.Page;
using TightWiki.Web.Filters;

namespace TightWiki.Controllers
{
    [Authorize]
    public class TagsController(ITagsBffService tagsBff) : Controller
    {
        [AllowAnonymous]
        [ProducesView]
        public BrowseViewModel Browse(string givenCanonical)
            => tagsBff.GetBrowseViewModel(givenCanonical);
    }
}
