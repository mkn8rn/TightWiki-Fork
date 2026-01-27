using BLL.Services.Configuration;
using BLL.Services.Emojis;
using BLL.Services.Exception;
using BLL.Services.Pages;
using BLL.Services.PageFile;
using BLL.Services.Spanned;
using BLL.Services.Statistics;
using BLL.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NTDLS.DelegateThreadPooling;
using NTDLS.Helpers;
using System.Reflection;
using TightWiki.Utils.Caching;
using TightWiki.Contracts;
using TightWiki.Contracts.DataModels;
using TightWiki.Web.Engine.Utility;
using TightWiki.Web.Engine.Library.Interfaces;
using TightWiki.Helpers;
using TightWiki.Localisation;
using TightWiki.Utils.Security;
using TightWiki.Static;
using TightWiki.Utils;
using TightWiki.Web.Bff.ViewModels;
using TightWiki.Web.Bff.ViewModels.Admin;
using TightWiki.Web.Bff.ViewModels.Page;
using TightWiki.Web.Bff.ViewModels.Utility;
using static TightWiki.Contracts.Constants;

namespace TightWiki.Controllers
{
[Authorize]
[Route("[controller]")]
public class AdminController : WikiControllerBase<AdminController>
{
    private readonly ITightEngine _tightEngine;
    private readonly IEngineConfiguration _engineConfig;
    private readonly IEngineDataProvider _engineDataProvider;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IStringLocalizer<AdminController> _localizer;
    private readonly IConfigurationService _configurationService;
    private readonly IEmojiService _emojiService;
    private readonly IExceptionService _exceptionService;
    private readonly IPageService _pageService;
    private readonly IPageFileService _pageFileService;
    private readonly ISpannedService _spannedService;
    private readonly IStatisticsService _statisticsService;
    private readonly IUsersService _usersService;

    public AdminController(
        ITightEngine tightEngine,
        IEngineConfigurationProvider engineConfigProvider,
        IEngineDataProvider engineDataProvider,
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IStringLocalizer<AdminController> localizer,
        IConfigurationService configurationService,
        IEmojiService emojiService,
        IExceptionService exceptionService,
        IPageService pageService,
        IPageFileService pageFileService,
        ISpannedService spannedService,
        IStatisticsService statisticsService,
        IUsersService usersService)
    : base(signInManager, userManager, localizer)
    {
        _tightEngine = tightEngine;
        _engineConfig = engineConfigProvider.GetEngineConfiguration();
        _engineDataProvider = engineDataProvider;
        _userManager = userManager;
        _localizer = localizer;
        _configurationService = configurationService;
        _emojiService = emojiService;
        _exceptionService = exceptionService;
        _pageService = pageService;
        _pageFileService = pageFileService;
        _spannedService = spannedService;
        _statisticsService = statisticsService;
        _usersService = usersService;
    }

    #region Metrics.

        [Authorize]
        [HttpGet("Database")]
        public ActionResult Database()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Database");

            var versions = _spannedService.GetDatabaseVersions();
            var pageCounts = _spannedService.GetDatabasePageCounts();
            var pageSizes = _spannedService.GetDatabasePageSizes();

            var info = new List<DatabaseInfo>();

            foreach (var version in versions)
            {
                var pageCount = pageCounts.FirstOrDefault(o => o.Name == version.Name).PageCount;
                var pageSize = pageSizes.FirstOrDefault(o => o.Name == version.Name).PageSize;

                info.Add(new DatabaseInfo
                {
                    Name = version.Name,
                    Version = version.Version,
                    PageCount = pageCount,
                    PageSize = pageSize,
                    DatabaseSize = pageCount * pageSize
                });
            }

            var model = new DatabaseViewModel()
            {
                Info = info
            };

            return View(model);
        }

        [Authorize]
        [HttpPost("Database/{databaseAction}/{database}")]
        public ActionResult Database(ConfirmActionViewModel model, string databaseAction, string database)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Database");

            if (model.UserSelection == true)
            {
                try
                {
                    switch (databaseAction)
                    {
                        case "Optimize":
                            {
                                var resultText = _spannedService.OptimizeDatabase(database);
                                return NotifyOfSuccess(Localize("Optimization complete. {0}", resultText), model.YesRedirectURL);
                            }
                        case "Vacuum":
                            {
                                var resultText = _spannedService.VacuumDatabase(database);
                                return NotifyOfSuccess(Localize("Vacuum complete. {0}", resultText), model.YesRedirectURL);
                            }
                        case "Verify":
                            {
                                var resultText = _spannedService.IntegrityCheckDatabase(database);
                                return NotifyOfSuccess(Localize("Verification complete. {0}", resultText), model.YesRedirectURL);
                            }
                    }
                }
                catch (Exception ex)
                {
                    return NotifyOfError(Localize("Operation failed: {0}", ex.Message), model.YesRedirectURL);
                }

                return NotifyOfError(Localize("Unknown database action: '{0}'", databaseAction), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        #endregion

        #region Metrics.

        [Authorize]
        [HttpGet("Metrics")]
        public ActionResult Metrics()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Metrics");

            var version = string.Join('.', (Assembly.GetExecutingAssembly()
                .GetName().Version?.ToString() ?? "0.0.0.0").Split('.').Take(3)); //Major.Minor.Patch

            var model = new MetricsViewModel()
            {
                Metrics = _configurationService.GetDatabaseStatistics(),
                ApplicationVersion = version
            };

            return View(model);
        }

        [Authorize]
        [HttpPost("PurgeCompilationStatistics")]
        public ActionResult PurgeCompilationStatistics(ConfirmActionViewModel model)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                _statisticsService.PurgeCompilationStatistics();
                return NotifyOfSuccess(Localize("Compilation statistics purged."), model.YesRedirectURL);
            }


            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("PurgeMemoryCache")]
        public ActionResult PurgeMemoryCache(ConfirmActionViewModel model)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                WikiCache.Clear();
                return NotifyOfSuccess(Localize("Memory cache purged."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        #endregion

        #region Compilation Statistics.

        [Authorize]
        [HttpGet("CompilationStatistics")]
        public ActionResult CompilationStatistics()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Compilations Statistics");

            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue<string>("OrderBy");
            var orderByDirection = GetQueryValue<string>("OrderByDirection");

            var model = new PageCompilationStatisticsViewModel()
            {
                Statistics = _statisticsService.GetCompilationStatisticsPaged(pageNumber, orderBy, orderByDirection),
            };

            model.PaginationPageCount = (model.Statistics.FirstOrDefault()?.PaginationPageCount ?? 0);

            model.Statistics.ForEach(o =>
            {
                o.LatestBuild = SessionState.LocalizeDateTime(o.LatestBuild);
            });

            return View(model);
        }

        #endregion

        #region Moderate.

        [Authorize]
        [HttpGet("Moderate")]
        public ActionResult Moderate()
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }

            SessionState.Page.Name = Localize("Page Moderation");

            var instruction = GetQueryValue<string>("Instruction");
            if (instruction != null)
            {
                var model = new PageModerateViewModel()
                {
                    Pages = _pageService.GetAllPagesByInstructionPaged(GetQueryValue("page", 1), instruction),
                    Instruction = instruction,
                    Instructions = typeof(WikiInstruction).GetProperties().Select(o => o.Name).ToList()
                };

                model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

                if (model.Pages != null && model.Pages.Count > 0)
                {
                    model.Pages.ForEach(o =>
                    {
                        o.CreatedDate = SessionState.LocalizeDateTime(o.CreatedDate);
                        o.ModifiedDate = SessionState.LocalizeDateTime(o.ModifiedDate);
                    });
                }

                return View(model);
            }

            return View(new PageModerateViewModel()
            {
                Pages = new(),
                Instruction = string.Empty,
                Instructions = typeof(WikiInstruction).GetProperties().Select(o => o.Name).ToList()
            });
        }

        #endregion

        #region Missing Pages.

        [Authorize]
        [HttpGet("MissingPages")]
        public ActionResult MissingPages()
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Missing Pages");

            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue<string>("OrderBy");
            var orderByDirection = GetQueryValue<string>("OrderByDirection");

            var model = new MissingPagesViewModel()
            {
                Pages = _pageService.GetMissingPagesPaged(pageNumber, orderBy, orderByDirection)
            };

            model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }

        #endregion

        #region Namespaces.

        [Authorize]
        [HttpGet("Namespaces")]
        public ActionResult Namespaces()
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Namespaces");

            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue<string>("OrderBy");
            var orderByDirection = GetQueryValue<string>("OrderByDirection");

            var model = new NamespacesViewModel()
            {
                Namespaces = _pageService.GetAllNamespacesPaged(pageNumber, orderBy, orderByDirection),
            };

            model.PaginationPageCount = (model.Namespaces.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }

        [Authorize]
        [HttpGet("Namespace/{namespaceName?}")]
        public ActionResult Namespace(string? namespaceName = null)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Namespace");

            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue<string>("OrderBy");
            var orderByDirection = GetQueryValue<string>("OrderByDirection");

            var model = new NamespaceViewModel()
            {
                Pages = _pageService.GetAllNamespacePagesPaged(pageNumber, namespaceName ?? string.Empty, orderBy, orderByDirection),
                Namespace = namespaceName ?? string.Empty
            };

            model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

            if (model.Pages != null && model.Pages.Count > 0)
            {
                model.Pages.ForEach(o =>
                {
                    o.CreatedDate = SessionState.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = SessionState.LocalizeDateTime(o.ModifiedDate);
                });
            }

            return View(model);
        }

        #endregion

        #region Pages.

        [Authorize]
        [HttpGet("Pages")]
        public ActionResult Pages()
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Pages");

            var searchString = GetQueryValue<string>("SearchString");
            var orderBy = GetQueryValue<string>("OrderBy");
            var orderByDirection = GetQueryValue<string>("OrderByDirection");

            var model = new PagesViewModel()
            {
                Pages = _pageService.GetAllPagesPaged(GetQueryValue("page", 1), orderBy, orderByDirection, Utility.SplitToTokens(searchString)),
                SearchString = searchString ?? string.Empty
            };

            model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

            if (model.Pages != null && model.Pages.Count > 0)
            {
                model.Pages.ForEach(o =>
                {
                    o.CreatedDate = SessionState.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = SessionState.LocalizeDateTime(o.ModifiedDate);
                });
            }

            return View(model);
        }

        #endregion

        #region Revisions.

        [Authorize]
        [HttpPost("RevertPageRevision/{givenCanonical}/{revision:int}")]
        public ActionResult Revert(string givenCanonical, int revision, ConfirmActionViewModel model)
        {
            try
            {
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.RequirePermission(null, WikiPermission.Moderate);

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            if (model.UserSelection == true)
            {
                var page = _pageService.GetPageRevisionByNavigation(pageNavigation, revision).EnsureNotNull();

                int currentPageRevision = _pageService.GetCurrentPageRevision(page.Id);
                if (revision >= currentPageRevision)
                {
                    return NotifyOfError(Localize("You cannot revert to the current page revision."));
                }

                var enginePage = MapToEnginePage(page);
                TightWiki.Web.Engine.Helpers.UpsertPage(_tightEngine, _engineConfig, _engineDataProvider, enginePage, SessionState);

                return NotifyOfSuccess(Localize("The page has been reverted."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpGet("DeletedPageRevisions/{pageId:int}")]
        public ActionResult DeletedPageRevisions(int pageId)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue<string>("OrderBy");
            var orderByDirection = GetQueryValue<string>("OrderByDirection");

            var model = new DeletedPagesRevisionsViewModel()
            {
                Revisions = _pageService.GetDeletedPageRevisionsByIdPaged(pageId, pageNumber, orderBy, orderByDirection)
            };

            var page = _pageService.GetLimitedPageInfoByIdAndRevision(pageId);
            if (page == null)
            {
                return NotifyOfError(Localize("The specified page could not be found."));
            }

            model.Name = page.Name;
            model.Namespace = page.Namespace;
            model.Navigation = page.Navigation;
            model.PageId = pageId;
            model.PaginationPageCount = (model.Revisions.FirstOrDefault()?.PaginationPageCount ?? 0);

            model.Revisions.ForEach(o =>
            {
                o.DeletedDate = SessionState.LocalizeDateTime(o.DeletedDate);
            });

            return View(model);
        }

        [Authorize]
        [HttpGet("DeletedPageRevision/{pageId:int}/{revision:int}")]
        public ActionResult DeletedPageRevision(int pageId, int revision)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var model = new DeletedPageRevisionViewModel();

            var page = _pageService.GetDeletedPageRevisionById(pageId, revision);

            if (page != null)
            {
                var state = _tightEngine.Transform(_engineConfig, _engineDataProvider, SessionState, page);
                model.PageId = pageId;
                model.Revision = pageId;
                model.Body = state.HtmlResult;
                model.DeletedDate = SessionState.LocalizeDateTime(page.DeletedDate);
                model.DeletedByUserName = page.DeletedByUserName;
            }

            return View(model);
        }

        [Authorize]
        [HttpGet("PageRevisions/{givenCanonical}")]
        public ActionResult PageRevisions(string givenCanonical)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue<string>("OrderBy");
            var orderByDirection = GetQueryValue<string>("OrderByDirection");

            var model = new PageRevisionsViewModel()
            {
                Revisions = _pageService.GetPageRevisionsInfoByNavigationPaged(pageNavigation, pageNumber, orderBy, orderByDirection)
            };

            model.PaginationPageCount = (model.Revisions.FirstOrDefault()?.PaginationPageCount ?? 0);

            model.Revisions.ForEach(o =>
            {
                o.CreatedDate = SessionState.LocalizeDateTime(o.CreatedDate);
                o.ModifiedDate = SessionState.LocalizeDateTime(o.ModifiedDate);
            });

            foreach (var p in model.Revisions)
            {
                var thisRev = _pageService.GetPageRevisionByNavigation(p.Navigation, p.Revision);
                var prevRev = _pageService.GetPageRevisionByNavigation(p.Navigation, p.Revision - 1);
                p.ChangeAnalysis = Differentiator.GetComparisonSummary(thisRev?.Body ?? "", prevRev?.Body ?? "");
            }

            if (model.Revisions != null && model.Revisions.Count > 0)
            {
                SessionState.SetPageId(model.Revisions.First().PageId);
            }

            return View(model);
        }

        [Authorize]
        [HttpPost("DeletePageRevision/{givenCanonical}/{revision:int}")]
        public ActionResult DeletePageRevision(ConfirmActionViewModel model, string givenCanonical, int revision)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            if (model.UserSelection == true)
            {
                var page = _pageService.GetPageInfoByNavigation(pageNavigation);
                if (page == null)
                {
                    return NotifyOfError(Localize("The page could not be found."));
                }

                int revisionCount = _pageService.GetPageRevisionCountByPageId(page.Id);
                if (revisionCount <= 1)
                {
                    return NotifyOfError(Localize("You cannot delete the only existing revision of a page, instead you would need to delete the entire page."));
                }

                //If we are deleting the latest revision, then we need to grab the previous
                //  version and make it the latest then delete the specified revision.
                if (revision >= page.Revision)
                {
                    int previousRevision = _pageService.GetPagePreviousRevision(page.Id, revision);
                    var previousPageRevision = _pageService.GetPageRevisionByNavigation(pageNavigation, previousRevision).EnsureNotNull();
                    var enginePage = MapToEnginePage(previousPageRevision);
                    TightWiki.Web.Engine.Helpers.UpsertPage(_tightEngine, _engineConfig, _engineDataProvider, enginePage, SessionState);
                }

                _pageService.MovePageRevisionToDeletedById(page.Id, revision, SessionState.Profile.EnsureNotNull().UserId);

                return NotifyOfSuccess(Localize("Page revision has been moved to the deletion queue."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }



        #endregion

        #region Deleted Pages.

        [Authorize]
        [HttpGet("DeletedPage/{pageId}")]
        public ActionResult DeletedPage(int pageId)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var model = new DeletedPageViewModel();

            var page = _pageService.GetDeletedPageById(pageId);

            if (page != null)
            {
                var state = _tightEngine.Transform(_engineConfig, _engineDataProvider, SessionState, page);
                model.PageId = pageId;
                model.Body = state.HtmlResult;
                model.DeletedDate = SessionState.LocalizeDateTime(page.ModifiedDate);
                model.DeletedByUserName = page.DeletedByUserName;
            }

            return View(model);
        }



        [Authorize]
        [HttpGet("DeletedPages")]
        public ActionResult DeletedPages()
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var searchString = GetQueryValue("SearchString", string.Empty);
            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue<string>("OrderBy");
            var orderByDirection = GetQueryValue<string>("OrderByDirection");

            var model = new DeletedPagesViewModel()
            {
                Pages = _pageService.GetAllDeletedPagesPaged(pageNumber, orderBy, orderByDirection, Utility.SplitToTokens(searchString)),
                SearchString = searchString
            };

            model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }


        [Authorize]
        [HttpPost("RebuildAllPages")]
        public ActionResult RebuildAllPages(ConfirmActionViewModel model)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                foreach (var page in _pageService.GetAllPages())
                {
                    var enginePage = MapToEnginePage(page);
                    TightWiki.Web.Engine.Helpers.RefreshPageMetadata(_tightEngine, _engineConfig, _engineDataProvider, enginePage, SessionState);
                }
                return NotifyOfSuccess(Localize("All pages have been rebuilt."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("PreCacheAllPages")]
        public ActionResult PreCacheAllPages(ConfirmActionViewModel model)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var pool = new DelegateThreadPool();


            if (model.UserSelection == true)
            {
                var workload = pool.CreateChildPool();

                foreach (var page in _pageService.GetAllPages())
                {
                    workload.Enqueue(() =>
                    {
                        string queryKey = string.Empty;
                        foreach (var query in Request.Query)
                        {
                            queryKey += $"{query.Key}:{query.Value}";
                        }

                        var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [page.Navigation, page.Revision, queryKey]);
                        if (WikiCache.Contains(cacheKey) == false)
                        {
                            var state = _tightEngine.Transform(_engineConfig, _engineDataProvider, SessionState, page, page.Revision);
                            page.Body = state.HtmlResult;

                            if (state.ProcessingInstructions.Contains(WikiInstruction.NoCache) == false)
                            {
                                WikiCache.Put(cacheKey, state.HtmlResult); //This is cleared with the call to Cache.ClearCategory($"Page:{page.Navigation}");
                            }
                        }
                    });
                }

                workload.WaitForCompletion();

                return NotifyOfSuccess(Localize("All pages have been cached."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("TruncatePageRevisions")]
        public ActionResult TruncatePageRevisions(ConfirmActionViewModel model)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                _pageService.TruncateAllPageRevisions("YES");
                WikiCache.Clear();
                return NotifyOfSuccess(Localize("All page revisions have been truncated."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("PurgeDeletedPageRevisions/{pageId:int}")]
        public ActionResult PurgeDeletedPageRevisions(ConfirmActionViewModel model, int pageId)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                _pageService.PurgeDeletedPageRevisionsByPageId(pageId);
                return NotifyOfSuccess(Localize("The page deletion queue has been purged."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("PurgeDeletedPageRevision/{pageId:int}/{revision:int}")]
        public ActionResult PurgeDeletedPageRevision(ConfirmActionViewModel model, int pageId, int revision)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                _pageService.PurgeDeletedPageRevisionByPageIdAndRevision(pageId, revision);
                return NotifyOfSuccess(Localize("The page revision has been purged from the deletion queue."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("RestoreDeletedPageRevision/{pageId:int}/{revision:int}")]
        public ActionResult RestoreDeletedPageRevision(ConfirmActionViewModel model, int pageId, int revision)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                _pageService.RestoreDeletedPageRevisionByPageIdAndRevision(pageId, revision);
                return NotifyOfSuccess(Localize("The page revision has been restored."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("PurgeDeletedPages")]
        public ActionResult PurgeDeletedPages(ConfirmActionViewModel model)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                _pageService.PurgeDeletedPages();
                return NotifyOfSuccess(Localize("The page deletion queue has been purged."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("PurgeDeletedPage/{pageId:int}")]
        public ActionResult PurgeDeletedPage(ConfirmActionViewModel model, int pageId)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                _pageService.PurgeDeletedPageByPageId(pageId);
                return NotifyOfSuccess(Localize("The page has been purged from the deletion queue."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("DeletePage/{pageId:int}")]
        public ActionResult DeletePage(ConfirmActionViewModel model, int pageId)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                _pageService.MovePageToDeletedById(pageId, SessionState.Profile.EnsureNotNull().UserId);
                return NotifyOfSuccess(Localize("The page has been moved to the deletion queue."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }


        [Authorize]
        [HttpPost("RestoreDeletedPage/{pageId:int}")]
        public ActionResult RestoreDeletedPage(ConfirmActionViewModel model, int pageId)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                _pageService.RestoreDeletedPageByPageId(pageId);
                var page = _pageService.GetLatestPageRevisionById(pageId);
                if (page != null)
                {
                    var enginePage = MapToEnginePage(page);
                    TightWiki.Web.Engine.Helpers.RefreshPageMetadata(_tightEngine, _engineConfig, _engineDataProvider, enginePage, SessionState);
                }
                return NotifyOfSuccess(Localize("The page has restored."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }


        #endregion

        #region Files.

        [Authorize]
        [HttpGet("OrphanedPageAttachments")]
        public ActionResult OrphanedPageAttachments()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Orphaned Page Attachments");

            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue<string>("OrderBy");
            var orderByDirection = GetQueryValue<string>("OrderByDirection");

            var model = new OrphanedPageAttachmentsViewModel()
            {
                Files = _pageFileService.GetOrphanedPageAttachmentsPaged(pageNumber, orderBy, orderByDirection),
            };

            model.PaginationPageCount = (model.Files.FirstOrDefault()?.PaginationPageCount ?? 0);

            /* Localization:
            if (model.Files != null && model.Files.Count > 0)
            {
                model.Files.ForEach(o =>
                {
                    o.CreatedDate = SessionState.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = SessionState.LocalizeDateTime(o.ModifiedDate);
                });
            }
            */

            return View(model);
        }

        [Authorize]
        [HttpPost("PurgeOrphanedAttachments")]
        public ActionResult PurgeOrphanedAttachments(ConfirmActionViewModel model)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                _pageFileService.PurgeOrphanedPageAttachments();
                return NotifyOfSuccess(Localize("All orphaned page attachments have been purged."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }


        [Authorize]
        [HttpPost("PurgeOrphanedAttachment/{pageFileId:int}/{revision:int}")]
        public ActionResult PurgeOrphanedAttachment(ConfirmActionViewModel model, int pageFileId, int revision)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                _pageFileService.PurgeOrphanedPageAttachment(pageFileId, revision);
                return NotifyOfSuccess(Localize("The pages orphaned attachments have been purged."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        #endregion

        #region Menu Items.

        [Authorize]
        [HttpGet("MenuItems")]
        public ActionResult MenuItems()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            //var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue<string>("OrderBy");
            var orderByDirection = GetQueryValue<string>("OrderByDirection");

            var model = new MenuItemsViewModel()
            {
                Items = _configurationService.GetAllMenuItems(orderBy, orderByDirection)
            };

            return View(model);
        }

        [Authorize]
        [HttpGet("MenuItem/{id:int?}")]
        public ActionResult MenuItem(int? id)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Menu Item");

            if (id != null)
            {
                var menuItem = _configurationService.GetMenuItemById((int)id);
                return View(MenuItemViewModel.FromDataModel(menuItem));
            }
            else
            {
                var model = new MenuItemViewModel
                {
                    Link = "/"
                };
                return View(model);
            }

        }

        /// <summary>
        /// Save site menu item.
        /// </summary>
        [Authorize]
        [HttpPost("MenuItem/{id:int?}")]
        public ActionResult MenuItem(int? id, MenuItemViewModel model)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (_configurationService.GetAllMenuItems().Where(o => o.Name.Equals(model.Name, StringComparison.InvariantCultureIgnoreCase) && o.Id != model.Id).Any())
            {
                ModelState.AddModelError("Name", Localize("The menu name '{0}' is already in use.", model.Name));
                return View(model);
            }

            if (id.DefaultWhenNull(0) == 0)
            {
                model.Id = _configurationService.CreateMenuItem(model.ToDataModel());
                ModelState.Clear();

                return NotifyOfSuccess(Localize("The menu item has been created."), $"/Admin/MenuItem/{model.Id}");
            }
            else
            {
                _configurationService.UpdateMenuItem(model.ToDataModel());
            }

            model.SuccessMessage = Localize("The menu item has been saved.");
            return View(model);
        }

        [Authorize]
        [HttpPost("DeleteMenuItem/{id:int}")]
        public ActionResult DeleteRole(ConfirmActionViewModel model, int id)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                _configurationService.DeleteMenuItem(id);
                return NotifyOfSuccess(Localize("The specified menu item has been deleted."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        #endregion

        #region Config.

        [Authorize]
        [HttpGet("Config")]
        public ActionResult Config()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var model = new ConfigurationViewModel()
            {
                Themes = _configurationService.GetAllThemes(),
                Roles = _usersService.GetAllRoles(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Nest = _configurationService.GetConfigurationNest()
            };
            return View(model);
        }

        [Authorize]
        [HttpPost("Config")]
        public ActionResult Config(ConfigurationViewModel model)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                model = new ConfigurationViewModel()
                {
                    Themes = _configurationService.GetAllThemes(),
                    Roles = _usersService.GetAllRoles(),
                    TimeZones = TimeZoneItem.GetAll(),
                    Countries = CountryItem.GetAll(),
                    Languages = LanguageItem.GetAll(),
                    Nest = _configurationService.GetConfigurationNest(),
                };

                var flatConfig = _configurationService.GetFlatConfiguration();

                foreach (var fc in flatConfig)
                {
                    var parent = model.Nest.Single(o => o.Name == fc.GroupName);
                    var child = parent.Entries.Single(o => o.Name == fc.EntryName);

                    var value = GetFormValue($"{fc.GroupId}:{fc.EntryId}", string.Empty);

                    //We keep the value in model.Nest.Entries.Value so that the page will reflect the new settings after post.
                    child.Value = value;

                    if (fc.IsRequired && string.IsNullOrEmpty(value))
                    {
                        model.ErrorMessage = Localize("{0} : {1} is required.", fc.GroupName, fc.EntryName);
                        return View(model);
                    }

                    if ($"{fc.GroupName}:{fc.EntryName}" == "Customization:Theme")
                    {
                        //This is not 100% necessary, I just want to prevent the user from needing to refresh to view the new theme.
                        GlobalConfiguration.SystemTheme = _configurationService.GetAllThemes().Single(o => o.Name == value);
                        if (string.IsNullOrEmpty(SessionState.Profile?.Theme))
                        {
                            SessionState.UserTheme = GlobalConfiguration.SystemTheme;
                        }
                    }

                    if (fc.IsEncrypted)
                    {
                        value = TightWiki.Utils.Security.Helpers.EncryptString(TightWiki.Utils.Security.Helpers.MachineKey, value);
                    }

                    _configurationService.SaveConfigurationValue(fc.GroupName, fc.EntryName, value);
                }

                WikiCache.ClearCategory(WikiCache.Category.Configuration);

                model.SuccessMessage = Localize("The configuration has been saved successfully!");
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.Message);
            }

            return View(model);
        }

        #endregion

        #region Emojis.

        [Authorize]
        [HttpGet("Emojis")]
        public ActionResult Emojis()
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Emojis");

            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue<string>("OrderBy");
            var orderByDirection = GetQueryValue<string>("OrderByDirection");
            var searchString = GetQueryValue("SearchString", string.Empty);

            var model = new EmojisViewModel()
            {
                Emojis = _emojiService.GetAllEmojisPaged(pageNumber, orderBy, orderByDirection, Utility.SplitToTokens(searchString)),
                SearchString = searchString
            };

            model.PaginationPageCount = (model.Emojis.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }

        [Authorize]
        [HttpGet("Emoji/{name}")]
        public ActionResult Emoji(string name)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var emoji = _emojiService.GetEmojiByName(name);

            var model = new EmojiViewModel
            {
                Emoji = emoji ?? new Emoji(),
                Categories = string.Join(",", _emojiService.GetEmojiCategoriesByName(name).Select(o => o.Category).ToList()),
                OriginalName = emoji?.Name ?? string.Empty
            };

            return View(model);
        }

        /// <summary>
        /// Update an existing emoji.
        /// </summary>
        [Authorize]
        [HttpPost("Emoji/{name}")]
        public ActionResult Emoji(EmojiViewModel model)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool nameChanged = false;

            if (!model.OriginalName.Equals(model.Emoji.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                nameChanged = true;
                var checkName = _emojiService.GetEmojiByName(model.Emoji.Name.ToLowerInvariant());
                if (checkName != null)
                {
                    ModelState.AddModelError("Emoji.Name", Localize("Emoji name is already in use."));
                    return View(model);
                }
            }

            var emoji = new UpsertEmoji
            {
                Id = model.Emoji.Id,
                Name = model.Emoji.Name.ToLowerInvariant(),
                Categories = Utility.SplitToTokens($"{model.Categories} {model.Emoji.Name} {Text.SeparateCamelCase(model.Emoji.Name)}")
            };

            var file = Request.Form.Files["ImageData"];
            if (file != null && file.Length > 0)
            {
                if (file.Length > GlobalConfiguration.MaxEmojiFileSize)
                {
                    model.ErrorMessage += Localize("Could not save the attached image, too large.");
                }
                else
                {
                    try
                    {
                        emoji.ImageData = WebUtility.ConvertHttpFileToBytes(file);
                        _ = SixLabors.ImageSharp.Image.Load(new MemoryStream(emoji.ImageData));
                        emoji.MimeType = file.ContentType;
                    }
                    catch
                    {
                        model.ErrorMessage += Localize("Could not save the attached image.");
                    }
                }
            }

            emoji.Id = _emojiService.UpsertEmoji(emoji);
            model.OriginalName = model.Emoji.Name;
            model.SuccessMessage = Localize("The emoji has been saved successfully!");
            model.Emoji.Id = (int)emoji.Id;
            ModelState.Clear();

            if (nameChanged)
            {
                return NotifyOfSuccess(Localize("The emoji has been saved."), $"/Admin/Emoji/{Navigation.Clean(emoji.Name)}");
            }

            return View(model);
        }

        [Authorize]
        [HttpGet("AddEmoji")]
        public ActionResult AddEmoji()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var model = new AddEmojiViewModel()
            {
                Name = string.Empty,
                OriginalName = string.Empty,
                Categories = string.Empty
            };

            return View(model);
        }

        /// <summary>
        /// Save user profile.
        /// </summary>
        [Authorize]
        [HttpPost("AddEmoji")]
        public ActionResult AddEmoji(AddEmojiViewModel model)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (string.IsNullOrEmpty(model.OriginalName) == true || !model.OriginalName.Equals(model.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                var checkName = _emojiService.GetEmojiByName(model.Name.ToLowerInvariant());
                if (checkName != null)
                {
                    ModelState.AddModelError("Name", Localize("Emoji name is already in use."));
                    return View(model);
                }
            }

            var emoji = new UpsertEmoji
            {
                Id = model.Id,
                Name = model.Name.ToLowerInvariant(),
                Categories = Utility.SplitToTokens($"{model.Categories} {model.Name} {Text.SeparateCamelCase(model.Name)}")
            };

            var file = Request.Form.Files["ImageData"];
            if (file != null && file.Length > 0)
            {
                if (file.Length > GlobalConfiguration.MaxEmojiFileSize)
                {
                    ModelState.AddModelError("Name", Localize("Could not save the attached image, too large."));
                }
                else
                {
                    try
                    {
                        emoji.ImageData = WebUtility.ConvertHttpFileToBytes(file);
                        var image = SixLabors.ImageSharp.Image.Load(new MemoryStream(emoji.ImageData));
                        emoji.MimeType = file.ContentType;
                    }
                    catch
                    {
                        ModelState.AddModelError("Name", Localize("Could not save the attached image."));
                    }
                }
            }

            _emojiService.UpsertEmoji(emoji);

            return NotifyOfSuccess(Localize("The emoji has been created."), $"/Admin/Emoji/{Navigation.Clean(emoji.Name)}");
        }

        [Authorize]
        [HttpPost("DeleteEmoji/{name}")]
        public ActionResult DeleteEmoji(ConfirmActionViewModel model, string name)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var emoji = _emojiService.GetEmojiByName(name);

            if (model.UserSelection == true && emoji != null)
            {
                _emojiService.DeleteById(emoji.Id);
                return NotifyOfSuccess(Localize("The specified emoji has been deleted."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        #endregion

        #region Exceptions.

        [Authorize]
        [HttpGet("Exceptions")]
        public ActionResult Exceptions()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Exceptions");

            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue<string>("OrderBy");
            var orderByDirection = GetQueryValue<string>("OrderByDirection");

            var model = new ExceptionsViewModel()
            {
                Exceptions = _exceptionService.GetAllExceptionsPaged(pageNumber, orderBy, orderByDirection)
            };

            model.PaginationPageCount = (model.Exceptions.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }

        [Authorize]
        [HttpGet("Exception/{id}")]
        public ActionResult Exception(int id)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Exception");

            var model = new ExceptionViewModel()
            {
                Exception = _exceptionService.GetExceptionById(id)
            };

            return View(model);
        }

        [Authorize]
        [HttpPost("PurgeExceptions")]
        public ActionResult PurgeExceptions(ConfirmActionViewModel model)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                _exceptionService.PurgeExceptions();
                return NotifyOfSuccess(Localize("All exceptions have been purged."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        #endregion

        #region LDAP.

        public record LdapTestRequest(string Username, string Password);

        // POST: /Admin/TestLdap
        [HttpPost("TestLdap")]
        public async Task<IActionResult> TestLdap([FromBody] LdapTestRequest req)
        {
            var ldapAuthenticationConfiguration = _configurationService.GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.LDAPAuthentication);

            try
            {

                if (GlobalConfiguration.EnableLDAPAuthentication == false)
                {
                    return Json(new { ok = false, error = Localize("LDAP authentication is not enabled.") });
                }

                if (LDAPUtility.LdapCredentialChallenge(ldapAuthenticationConfiguration, StaticLocalizer.Localizer,
                    req.Username, req.Password, out var samAccountName, out var objectGuid))
                {
                    //We successfully authenticated against LDAP.

                    if (objectGuid == null || objectGuid == Guid.Empty)
                    {
                        return Json(new { ok = false, error = Localize("LDAP challenge succeeded, but the user does not have an objectGUID attribute.") });
                    }

                    var loginInfo = new UserLoginInfo("LDAP", objectGuid.Value.ToString(), "Active Directory");

                    var foundUser = await _userManager.FindByLoginAsync(loginInfo.LoginProvider, loginInfo.ProviderKey);

                    if (foundUser == null)
                    {
                        //User does not exist in TightWiki.
                        return Json(new { ok = true, message = Localize("LDAP challenge succeeded (un-provisioned account)."), distinguishedName = samAccountName });
                    }
                    else
                    {
                        if (_usersService.TryGetBasicProfileByUserId(Guid.Parse(foundUser.Id), out _))
                        {
                            //User and profile exist in TightWiki.
                            return Json(new { ok = true, message = Localize("LDAP challenge succeeded (fully provisioned account)."), distinguishedName = samAccountName });
                        }

                        //User exists in TightWiki, but the profile does not.
                        return Json(new { ok = true, message = Localize("LDAP challenge succeeded (partially provisioned account)."), distinguishedName = samAccountName });
                    }
                }
                return Json(new { ok = false, error = Localize("LDAP challenge failed.") });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = Localize("LDAP error: {0}.", ex.Message) });
            }
        }

        #endregion


        #region Private Helpers

        /// <summary>
        /// Maps a Contracts.Page to a Contracts.EnginePage for use with Engine helpers.
        /// </summary>
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
