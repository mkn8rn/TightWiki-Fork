using BLL.Services.Configuration;
using BLL.Services.Pages;
using BLL.Services.Users;
using DiffPlex.DiffBuilder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using NTDLS.Helpers;
using System.Globalization;
using System.Text;
using System.Xml.Serialization;
using TightWiki.Contracts;
using TightWiki.Contracts.DataModels;
using TightWiki.Contracts.Interfaces;
using TightWiki.Utils;
using TightWiki.Utils.Caching;
using TightWiki.Web.Bff.Extensions;
using TightWiki.Web.Bff.Interfaces;
using TightWiki.Web.Bff.ViewModels.Page;
using TightWiki.Web.Engine;
using TightWiki.Web.Engine.Library.Interfaces;
using TightWiki.Web.Engine.Utility;
using static TightWiki.Contracts.Constants;

namespace TightWiki.Web.Bff.Services
{
    public class PageBffService(
        IPageService pageService,
        IConfigurationService configurationService,
        IUsersService usersService,
        ITightEngine engine,
        IEngineConfigurationProvider configProvider,
        IEngineDataProvider dataProvider,
        ISideBySideDiffBuilder diffBuilder,
        ISessionState session,
        IStringLocalizer<PageBffService> localizer,
        IHttpContextAccessor httpContextAccessor,
        IOptions<RequestLocalizationOptions> localizationOptions)
        : IPageBffService
    {
        private readonly IEngineConfiguration _engineConfig = configProvider.GetEngineConfiguration();

        #region Display

        public DisplayPageResult GetDisplayResult(PageDisplayRequest request)
        {
            var result = new DisplayPageResult();
            var navigation = new NamespaceNavigation(request.GivenCanonical);

            string queryKey = string.Empty;

            var page = pageService.GetPageRevisionByNavigation(navigation.Canonical, request.PageRevision);
            if (page != null)
            {
                var instructions = pageService.GetPageProcessingInstructionsByPageId(page.Id);
                result.PageFound = true;
                result.PageId = page.Id;
                result.Model.Revision = page.Revision;
                result.Model.MostCurrentRevision = page.MostCurrentRevision;
                result.Model.Name = page.Name;
                result.Model.Namespace = page.Namespace;
                result.Model.Navigation = page.Navigation;
                result.Model.HideFooterComments = instructions.Contains(WikiInstruction.HideFooterComments);
                result.Model.HideFooterLastModified = instructions.Contains(WikiInstruction.HideFooterLastModified);
                result.Model.ModifiedByUserName = page.ModifiedByUserName;
                result.Model.ModifiedDate = session.LocalizeDateTime(page.ModifiedDate);

                if (GlobalConfiguration.PageCacheSeconds > 0)
                {
                    var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [page.Navigation, page.Revision, queryKey]);
                    if (WikiCache.TryGet<PageCache>(cacheKey, out var cached))
                    {
                        result.Model.Body = cached.Body;
                        result.PageTitle = cached.PageTitle;
                        WikiCache.Put(cacheKey, cached);
                    }
                    else
                    {
                        var state = engine.Transform(_engineConfig, dataProvider, session, page, request.PageRevision);
                        result.PageTitle = state.PageTitle;
                        result.Model.Body = state.HtmlResult;

                        if (state.ProcessingInstructions.Contains(WikiInstruction.NoCache) == false)
                        {
                            WikiCache.Put(cacheKey, new PageCache(state.HtmlResult) { PageTitle = state.PageTitle });
                        }
                    }
                }
                else
                {
                    var state = engine.Transform(_engineConfig, dataProvider, session, page, request.PageRevision);
                    result.Model.Body = state.HtmlResult;
                }

                if (GlobalConfiguration.EnablePageComments && GlobalConfiguration.ShowCommentsOnPageFooter && result.Model.HideFooterComments == false)
                {
                    PopulateComments(result.Model.Comments, navigation.Canonical, 1);
                }
            }
            else if (request.PageRevision != null)
            {
                result.PageFound = false;
                var notExistPageName = configurationService.GetConfigurationValue<string>(Constants.ConfigurationGroup.Customization, "Revision Does Not Exists Page");
                string notExistPageNavigation = NamespaceNavigation.CleanAndValidate(notExistPageName);
                var notExistsPage = pageService.GetPageRevisionByNavigation(notExistPageNavigation)
                    ?? throw new InvalidOperationException("Revision-not-exists page not found.");

                var state = engine.Transform(_engineConfig, dataProvider, session, notExistsPage);
                result.OverridePageName = notExistsPage.Name;
                result.Model.Body = state.HtmlResult;
                result.Model.HideFooterComments = true;
                result.ShouldCreatePage = false;
            }
            else
            {
                result.PageFound = false;
                var notExistPageName = configurationService.GetConfigurationValue<string>(Constants.ConfigurationGroup.Customization, "Page Not Exists Page");
                string notExistPageNavigation = NamespaceNavigation.CleanAndValidate(notExistPageName);
                var notExistsPage = pageService.GetPageRevisionByNavigation(notExistPageNavigation)
                    ?? throw new InvalidOperationException("Page-not-exists page not found.");

                var state = engine.Transform(_engineConfig, dataProvider, session, notExistsPage);
                result.OverridePageName = notExistsPage.Name;
                result.Model.Body = state.HtmlResult;
                result.Model.HideFooterComments = true;
                result.ShouldCreatePage = true;
            }

            // Apply session state changes that the controller used to do.
            if (result.PageFound)
            {
                session.SetPageId(result.PageId, request.PageRevision);
            }
            else
            {
                session.SetPageId(null, request.PageRevision);
                if (result.OverridePageName != null)
                    session.Page.Name = result.OverridePageName;
                if (result.ShouldCreatePage != null && session.IsAuthenticated && session.HoldsPermission(request.GivenCanonical, WikiPermission.Create))
                    session.ShouldCreatePage = result.ShouldCreatePage.Value;
            }

            if (result.PageTitle != null)
                session.PageTitle = result.PageTitle;

            return result;
        }

        #endregion

        #region Search

        public IActionResult AutoCompletePage(string? query)
        {
            var results = pageService.AutoCompletePage(query)
                .Select(o => new { text = o.Name, id = o.Navigation }).ToList();
            return new JsonResult(results);
        }

        public PageSearchViewModel GetSearchViewModel(PageSearchRequest request)
        {
            if (!string.IsNullOrEmpty(request.SearchString))
            {
                var pages = pageService.PageSearchPaged(Utility.SplitToTokens(request.SearchString), request.Page);
                return new PageSearchViewModel
                {
                    Pages = pages,
                    SearchString = request.SearchString,
                    PaginationPageCount = pages.FirstOrDefault()?.PaginationPageCount ?? 0
                };
            }

            return new PageSearchViewModel { Pages = new(), SearchString = request.SearchString ?? string.Empty };
        }

        #endregion

        #region Localization

        public PageLocalizationViewModel GetLocalizationViewModel()
        {
            var httpContext = httpContextAccessor.HttpContext;
            var referrer = httpContext?.Request.Headers.Referer.ToString() ?? string.Empty;

            var languages = (localizationOptions.Value.SupportedUICultures ?? [])
                .OrderBy(x => x.EnglishName, StringComparer.Create(CultureInfo.CurrentUICulture, ignoreCase: true)).ToList();

            return new PageLocalizationViewModel
            {
                Languages = languages,
                ReturnUrl = string.IsNullOrEmpty(referrer) ? string.Empty : referrer
            };
        }

        public IActionResult SetLocalization(SetLocalizationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Culture) || string.IsNullOrWhiteSpace(request.ReturnUrl))
                return new BadRequestResult();

            if (session.IsAuthenticated)
            {
                var profile = usersService.GetAccountProfileByUserId(session.Profile.EnsureNotNull().UserId);
                profile.Language = request.Culture;
                usersService.UpdateProfile(profile);
            }

            httpContextAccessor.HttpContext?.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(request.Culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Strict,
                    Secure = true,
                    HttpOnly = false
                });

            return new RedirectResult(request.ReturnUrl);
        }

        #endregion

        #region Comments

        public PageCommentsViewModel GetCommentsViewModel(PageCommentsRequest request)
        {
            session.RequirePermission(request.GivenCanonical, WikiPermission.Read);

            var pageNavigation = NamespaceNavigation.CleanAndValidate(request.GivenCanonical);

            if (!string.IsNullOrEmpty(request.Delete) && session.IsAuthenticated)
            {
                var commentId = int.Parse(request.Delete);
                var pageInfo = pageService.GetPageInfoByNavigation(pageNavigation)
                    ?? throw new InvalidOperationException("Page not found.");

                if (session.HoldsPermission(pageNavigation, WikiPermission.Moderate))
                    pageService.DeletePageCommentById(pageInfo.Id, commentId);
                else
                    pageService.DeletePageCommentByUserAndId(pageInfo.Id, session.Profile!.UserId, commentId);
            }

            var model = new PageCommentsViewModel();
            PopulateComments(model.Comments, pageNavigation, request.Page);
            model.PaginationPageCount = model.Comments.FirstOrDefault()?.PaginationPageCount ?? 0;
            return model;
        }

        public PageCommentsViewModel PostComment(PostCommentRequest request)
        {
            session.RequirePermission(request.GivenCanonical, WikiPermission.Read);

            var pageNavigation = NamespaceNavigation.CleanAndValidate(request.GivenCanonical);
            var pageInfo = pageService.GetPageInfoByNavigation(pageNavigation)
                ?? throw new InvalidOperationException("Page not found.");

            pageService.InsertPageComment(pageInfo.Id, session.Profile.EnsureNotNull().UserId, request.Model.Comment);

            var model = new PageCommentsViewModel();
            PopulateComments(model.Comments, pageNavigation, request.Page);
            model.PaginationPageCount = model.Comments.FirstOrDefault()?.PaginationPageCount ?? 0;
            return model;
        }

        #endregion

        #region Refresh

        public IActionResult RefreshPage(string givenCanonical)
        {
            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);
            var page = pageService.GetPageRevisionByNavigation(pageNavigation, null, true);
            if (page != null)
            {
                var enginePage = MapToEnginePage(page);
                TightWiki.Web.Engine.Helpers.RefreshPageMetadata(engine, _engineConfig, dataProvider, enginePage, session);
            }

            return new RedirectResult($"{GlobalConfiguration.BasePath}/{pageNavigation}");
        }

        #endregion

        #region Compare

        public PageCompareViewModel GetCompareViewModel(PageCompareRequest request)
        {
            session.RequirePermission(request.GivenCanonical, WikiPermission.Read);

            var pageNavigation = NamespaceNavigation.CleanAndValidate(request.GivenCanonical);
            var thisRev = pageService.GetPageRevisionByNavigation(pageNavigation, request.PageRevision);
            var prevRev = pageService.GetPageRevisionByNavigation(pageNavigation, request.PageRevision - 1);

            return new PageCompareViewModel
            {
                MostCurrentRevision = thisRev?.MostCurrentRevision,
                ModifiedByUserName = thisRev?.ModifiedByUserName ?? string.Empty,
                ThisRevision = thisRev?.Revision,
                PreviousRevision = prevRev?.Revision,
                DiffModel = diffBuilder.BuildDiffModel(prevRev?.Body ?? string.Empty, thisRev?.Body ?? string.Empty),
                ModifiedDate = session.LocalizeDateTime(thisRev?.ModifiedDate ?? DateTime.MinValue),
                ChangeSummary = thisRev?.ChangeSummary ?? string.Empty,
                ChangeAnalysis = Differentiator.GetComparisonSummary(thisRev?.Body ?? "", prevRev?.Body ?? "")
            };
        }

        #endregion

        #region Revisions

        public RevisionsViewModel GetRevisionsViewModel(PageRevisionsRequest request)
        {
            session.RequirePermission(request.GivenCanonical, WikiPermission.Read);

            var pageNavigation = NamespaceNavigation.CleanAndValidate(request.GivenCanonical);
            var revisions = pageService.GetPageRevisionsInfoByNavigationPaged(pageNavigation, request.Page, request.OrderBy, request.OrderByDirection);

            revisions.ForEach(o =>
            {
                o.CreatedDate = session.LocalizeDateTime(o.CreatedDate);
                o.ModifiedDate = session.LocalizeDateTime(o.ModifiedDate);
            });

            foreach (var p in revisions)
            {
                var thisRev = pageService.GetPageRevisionByNavigation(p.Navigation, p.Revision);
                var prevRev = pageService.GetPageRevisionByNavigation(p.Navigation, p.Revision - 1);
                p.ChangeAnalysis = Differentiator.GetComparisonSummary(thisRev?.Body ?? "", prevRev?.Body ?? "");
            }

            return new RevisionsViewModel
            {
                Revisions = revisions,
                PaginationPageCount = revisions.FirstOrDefault()?.PaginationPageCount ?? 0
            };
        }

        #endregion

        #region Delete

        public PageDeleteViewModel GetDeleteViewModel(string givenCanonical)
        {
            session.RequirePermission(givenCanonical, WikiPermission.Delete);

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);
            CheckNotProtected(pageNavigation);

            var page = pageService.GetPageRevisionByNavigation(pageNavigation)
                ?? throw new InvalidOperationException("Page not found.");

            session.SetPageId(null);

            return new PageDeleteViewModel
            {
                CountOfAttachments = pageService.GetCountOfPageAttachmentsById(page.Id),
                PageName = page.Name,
                MostCurrentRevision = page.Revision,
                PageRevision = page.Revision
            };
        }

        public IActionResult DeletePage(PageDeleteRequest request)
        {
            session.RequirePermission(request.GivenCanonical, WikiPermission.Delete);

            var pageNavigation = NamespaceNavigation.CleanAndValidate(request.GivenCanonical);
            CheckNotProtected(pageNavigation);

            if (!request.IsActionConfirmed)
                return new RedirectResult($"{GlobalConfiguration.BasePath}/{pageNavigation}");

            var page = pageService.GetPageRevisionByNavigation(pageNavigation)
                ?? throw new InvalidOperationException("Page not found.");

            pageService.MovePageToDeletedById(page.Id, (session.Profile?.UserId).EnsureNotNullOrEmpty());
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Navigation]));
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Id]));

            return NotifySuccess(localizer.Localize("The page has been deleted."), "/Home");
        }

        #endregion

        #region Revert

        public PageRevertViewModel GetRevertViewModel(string givenCanonical, int pageRevision)
        {
            session.RequirePermission(givenCanonical, WikiPermission.Moderate);

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var mostCurrentPage = pageService.GetPageRevisionByNavigation(pageNavigation)
                ?? throw new InvalidOperationException("Page not found.");

            var revisionPage = pageService.GetPageRevisionByNavigation(pageNavigation, pageRevision)
                ?? throw new InvalidOperationException("Page revision not found.");

            return new PageRevertViewModel
            {
                PageName = revisionPage.Name,
                HighestRevision = mostCurrentPage.Revision,
                HigherRevisionCount = revisionPage.HigherRevisionCount,
            };
        }

        public IActionResult RevertPage(PageRevertRequest request)
        {
            session.RequirePermission(request.GivenCanonical, WikiPermission.Moderate);

            var pageNavigation = NamespaceNavigation.CleanAndValidate(request.GivenCanonical);

            if (!request.IsActionConfirmed)
                return new RedirectResult($"{GlobalConfiguration.BasePath}/{pageNavigation}");

            var page = pageService.GetPageRevisionByNavigation(pageNavigation, request.PageRevision)
                ?? throw new InvalidOperationException("Page revision not found.");

            var enginePage = MapToEnginePage(page);
            TightWiki.Web.Engine.Helpers.UpsertPage(engine, _engineConfig, dataProvider, enginePage, session);

            return NotifySuccess(localizer.Localize("The page has been reverted."), $"/{pageNavigation}");
        }

        #endregion

        #region Edit

        public PageEditViewModel GetEditViewModel(string givenCanonical, string? pageName)
        {
            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);
            session.RequirePermission(pageNavigation, WikiPermission.Edit);

            var featureTemplates = pageService.GetAllFeatureTemplates();
            var page = pageService.GetPageRevisionByNavigation(pageNavigation);

            if (page != null)
            {
                return new PageEditViewModel
                {
                    Id = page.Id,
                    Body = page.Body,
                    Name = page.Name,
                    Navigation = NamespaceNavigation.CleanAndValidate(page.Navigation),
                    Description = page.Description,
                    FeatureTemplates = featureTemplates
                };
            }

            string templateName = configurationService.GetConfigurationValue<string>(Constants.ConfigurationGroup.Customization, "New Page Template")
                ?? string.Empty;
            string templateNavigation = NamespaceNavigation.CleanAndValidate(templateName);
            var templatePage = pageService.GetPageRevisionByNavigation(templateNavigation);

            var templates = pageService.GetAllTemplatePages();
            if (templatePage != null)
                templates.Insert(0, templatePage);

            var model = new PageEditViewModel
            {
                Body = templatePage?.Body ?? string.Empty,
                Name = pageNavigation.Replace('_', ' '),
                Navigation = NamespaceNavigation.CleanAndValidate(pageNavigation),
                Templates = templates,
                FeatureTemplates = featureTemplates
            };

            if (!string.IsNullOrEmpty(pageName) && model.Id == 0)
                model.Name = pageName.Replace('_', ' ');

            return model;
        }

        public IActionResult SavePage(PageEditViewModel model)
        {
            var navigation = NamespaceNavigation.CleanAndValidate(model.Name);
            var permission = model.Id == 0 ? WikiPermission.Create : WikiPermission.Edit;
            session.RequirePermission(navigation, permission);

            var request = new SavePageRequest
            {
                Model = model,
                UserId = session.Profile.EnsureNotNull().UserId
            };

            var result = ExecuteSavePage(request);

            if (result.ErrorMessage != null)
                throw new InvalidOperationException(result.ErrorMessage);

            if (result.ModelErrors.Count > 0)
                throw new InvalidOperationException(result.ModelErrors.Values.First());

            if (result.IsNew)
                return NotifySuccess(localizer.Localize(result.SuccessMessage!), $"/{result.Navigation}/Edit");

            if (result.OriginalNavigation != null)
                return new RedirectResult($"{GlobalConfiguration.BasePath}/{result.Navigation}/Edit");

            return NotifySuccess(localizer.Localize(result.SuccessMessage!), $"/{result.Navigation}/Edit");
        }

        public IActionResult GetTemplateBody(int id)
        {
            var body = pageService.GetPageRevisionById(id)?.Body;
            return new JsonResult(new { body = body ?? "" });
        }

        #endregion

        #region Export

        public IActionResult Export(PageExportRequest request)
        {
            session.RequirePermission(request.GivenCanonical, WikiPermission.Read);

            var page = pageService.GetPageRevisionByNavigation(new NamespaceNavigation(request.GivenCanonical).Canonical)
                ?? throw new FileNotFoundException("Page not found.");

            var sr = new StringWriter();
            var writer = new System.Xml.XmlTextWriter(sr);
            var serializer = new XmlSerializer(typeof(Page));
            serializer.Serialize(writer, page);

            return new FileContentResult(Encoding.UTF8.GetBytes(sr.ToString()), "text/xml")
            {
                FileDownloadName = $"{request.GivenCanonical}.xml"
            };
        }

        #endregion

        #region Private Helpers

        private static RedirectResult NotifySuccess(string message, string redirectUrl)
            => new($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{redirectUrl}")}&RedirectTimeout=5");

        private void CheckNotProtected(string pageNavigation)
        {
            var page = pageService.GetPageRevisionByNavigation(pageNavigation);
            if (page == null) return;
            var instructions = pageService.GetPageProcessingInstructionsByPageId(page.Id);
            if (instructions.Contains(WikiInstruction.Protect))
                throw new InvalidOperationException(
                    localizer.Localize("The page is protected and cannot be deleted. A moderator or an administrator must remove the protection before deletion."));
        }

        private SavePageResult ExecuteSavePage(SavePageRequest request)
        {
            var model = request.Model;
            var result = new SavePageResult();

            if (GlobalConfiguration.ShowChangeSummaryWhenEditing
                && GlobalConfiguration.RequireChangeSummaryWhenEditing
                && string.IsNullOrEmpty(model.ChangeSummary))
            {
                result.ModelErrors["ChangeSummary"] = "A change summary is required for page edits.";
                return result;
            }

            if (Utility.PageNameContainsUnsafeCharacters(model.Name))
            {
                result.ModelErrors["Name"] = $"The page name contains characters which are disallowed: {string.Join(' ', Utility.UnsafePageNameCharacters)}";
                return result;
            }

            if (Utility.CountOccurrencesOf(model.Name, "::") > 1)
            {
                result.ModelErrors["Name"] = "The characters '::' are used to denote a namespace name. A page name cannot contain more than one set of these characters.";
                return result;
            }

            if (model.Id == 0) // New page
            {
                var navigation = NamespaceNavigation.CleanAndValidate(model.Name);

                if (pageService.GetPageInfoByNavigation(navigation) != null)
                {
                    result.ModelErrors["Name"] = "The page name you entered already exists.";
                    return result;
                }

                var page = new Page
                {
                    CreatedDate = DateTime.UtcNow,
                    CreatedByUserId = request.UserId,
                    ModifiedDate = DateTime.UtcNow,
                    ModifiedByUserId = request.UserId,
                    Body = model.Body ?? "",
                    Name = model.Name,
                    ChangeSummary = model.ChangeSummary ?? string.Empty,
                    Navigation = navigation,
                    Description = model.Description ?? ""
                };

                var enginePage = MapToEnginePage(page);
                page.Id = TightWiki.Web.Engine.Helpers.UpsertPage(engine, _engineConfig, dataProvider, enginePage, session);

                result.Success = true;
                result.IsNew = true;
                result.PageId = page.Id;
                result.Navigation = page.Navigation;
                result.SuccessMessage = "The page has been created.";
            }
            else // Existing page
            {
                var navigation = NamespaceNavigation.CleanAndValidate(model.Name);
                var page = pageService.GetPageRevisionById(model.Id)
                    ?? throw new InvalidOperationException("Page not found.");

                var instructions = pageService.GetPageProcessingInstructionsByPageId(page.Id);
                if (instructions.Contains(WikiInstruction.Protect) && !session.HoldsPermission(navigation, WikiPermission.Moderate))
                {
                    result.ErrorMessage = "The page is protected and cannot be modified except by a moderator or an administrator unless the protection is removed.";
                    return result;
                }

                if (!page.Navigation.Equals(navigation, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (pageService.GetPageInfoByNavigation(navigation) != null)
                    {
                        result.ModelErrors["Name"] = "The page name you entered already exists.";
                        return result;
                    }
                    result.OriginalNavigation = page.Navigation;
                }

                page.ModifiedDate = DateTime.UtcNow;
                page.ModifiedByUserId = request.UserId;
                page.Body = model.Body ?? "";
                page.Name = model.Name;
                page.ChangeSummary = model.ChangeSummary ?? string.Empty;
                page.Navigation = navigation;
                page.Description = model.Description ?? "";

                var enginePage = MapToEnginePage(page);
                TightWiki.Web.Engine.Helpers.UpsertPage(engine, _engineConfig, dataProvider, enginePage, session);

                if (!string.IsNullOrWhiteSpace(result.OriginalNavigation))
                {
                    WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [result.OriginalNavigation]));
                    WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Id]));
                }

                result.Success = true;
                result.PageId = page.Id;
                result.Navigation = page.Navigation;
                result.SuccessMessage = "The page was saved.";
            }

            return result;
        }

        private void PopulateComments(List<PageComment> target, string pageNavigation, int pageNumber)
        {
            var comments = pageService.GetPageCommentsPaged(pageNavigation, pageNumber);
            foreach (var comment in comments)
            {
                target.Add(new PageComment
                {
                    PaginationPageCount = comment.PaginationPageCount,
                    UserNavigation = comment.UserNavigation,
                    Id = comment.Id,
                    UserName = comment.UserName,
                    UserId = comment.UserId,
                    Body = WikifierLite.Process(comment.Body, _engineConfig),
                    CreatedDate = session.LocalizeDateTime(comment.CreatedDate)
                });
            }
        }

        private static EnginePage MapToEnginePage(Page page) => new()
        {
            Id = page.Id,
            Revision = page.Revision,
            MostCurrentRevision = page.MostCurrentRevision,
            Name = page.Name,
            Navigation = page.Navigation,
            Description = page.Description,
            Body = page.Body,
            CreatedDate = page.CreatedDate,
            ModifiedDate = page.ModifiedDate,
            Match = page.Match,
            Weight = page.Weight,
            Score = page.Score,
            PaginationPageCount = page.PaginationPageCount
        };

        #endregion
    }
}
