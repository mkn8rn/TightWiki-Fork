using BLL.Services.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Text;
using TightWiki.Contracts;
using TightWiki.Web.Engine.Utility;
using TightWiki.Web.Engine.Library.Interfaces;
using TightWiki.Utils;
using TightWiki.Web.Bff.ViewModels.Page;
using static TightWiki.Contracts.Constants;



namespace TightWiki.Controllers
{
    [Authorize]
    public class TagsController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IStringLocalizer<TagsController> localizer,
        IPageService pageService,
        IEngineDataProvider engineDataProvider)
        : WikiControllerBase<TagsController>(signInManager, userManager, localizer)
    {
        [AllowAnonymous]
        public ActionResult Browse(string givenCanonical)
        {
            try
            {
                SessionState.RequirePermission(givenCanonical, WikiPermission.Read);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Tags");

            givenCanonical = NamespaceNavigation.CleanAndValidate(givenCanonical);

            string glossaryName = "glossary_" + (new Random()).Next(0, 1000000).ToString();
            var pages = pageService.GetPageInfoByTag(givenCanonical).OrderBy(o => o.Name).ToList();
            var glossaryHtml = new StringBuilder();
            var alphabet = pages.Select(p => p.Name.Substring(0, 1).ToUpperInvariant()).Distinct();

            if (pages.Count > 0)
            {
                glossaryHtml.Append("<center>");
                foreach (var alpha in alphabet)
                {
                    glossaryHtml.Append("<a href=\"#" + glossaryName + "_" + alpha + "\">" + alpha + "</a>&nbsp;");
                }
                glossaryHtml.Append("</center>");

                glossaryHtml.Append("<ul>");
                foreach (var alpha in alphabet)
                {
                    glossaryHtml.Append("<li><a name=\"" + glossaryName + "_" + alpha + "\">" + alpha + "</a></li>");

                    glossaryHtml.Append("<ul>");
                    foreach (var page in pages.Where(p => p.Name.StartsWith(alpha, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        glossaryHtml.Append("<li><a href=\"/" + page.Navigation + "\">" + page.Name + "</a>");

                        if (page.Description.Length > 0)
                        {
                            glossaryHtml.Append(" - " + page.Description);
                        }
                        glossaryHtml.Append("</li>");
                    }
                    glossaryHtml.Append("</ul>");
                }

                glossaryHtml.Append("</ul>");
            }

            var model = new BrowseViewModel
            {
                AssociatedPages = glossaryHtml.ToString(),
                TagCloud = TagCloud.Build(GlobalConfiguration.BasePath, engineDataProvider, givenCanonical, 100)
            };

            return View(model);
        }
    }
}
