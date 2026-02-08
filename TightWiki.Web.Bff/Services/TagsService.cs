using BLL.Services.Pages;
using Microsoft.Extensions.Localization;
using System.Text;
using TightWiki.Contracts;
using TightWiki.Contracts.Interfaces;
using TightWiki.Utils;
using TightWiki.Web.Bff.Extensions;
using TightWiki.Web.Bff.Interfaces;
using TightWiki.Web.Bff.ViewModels.Page;
using TightWiki.Web.Engine.Library.Interfaces;
using TightWiki.Web.Engine.Utility;
using static TightWiki.Contracts.Constants;

namespace TightWiki.Web.Bff.Services
{
    public class TagsService(
        IPageService pageService,
        IEngineDataProvider dataProvider,
        ISessionState session,
        IStringLocalizer<TagsService> localizer)
        : ITagsBffService
    {
        public BrowseViewModel GetBrowseViewModel(string givenCanonical)
        {
            session.RequirePermission(givenCanonical, WikiPermission.Read);
            session.Page.Name = localizer.Localize("Tags");

            var tag = NamespaceNavigation.CleanAndValidate(givenCanonical);

            string glossaryName = "glossary_" + Random.Shared.Next(0, 1000000).ToString();
            var pages = pageService.GetPageInfoByTag(tag).OrderBy(o => o.Name).ToList();
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

            return new BrowseViewModel
            {
                AssociatedPages = glossaryHtml.ToString(),
                TagCloud = TagCloud.Build(GlobalConfiguration.BasePath, dataProvider, tag, 100)
            };
        }
    }
}
