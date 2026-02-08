using BLL.Services.Configuration;
using BLL.Services.Emojis;
using BLL.Services.Exception;
using BLL.Services.Pages;
using BLL.Services.PageFile;
using BLL.Services.Spanned;
using BLL.Services.Statistics;
using BLL.Services.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NTDLS.Helpers;
using System.Reflection;
using TightWiki.Contracts;
using TightWiki.Contracts.DataModels;
using TightWiki.Contracts.Interfaces;
using TightWiki.Contracts.Responses;
using TightWiki.Localisation;
using TightWiki.Static;
using TightWiki.Utils;
using TightWiki.Utils.Caching;
using TightWiki.Utils.Security;
using TightWiki.Web.Bff.Extensions;
using TightWiki.Web.Bff.Interfaces;
using TightWiki.Web.Bff.ViewModels.Admin;
using TightWiki.Web.Bff.ViewModels.Page;
using TightWiki.Web.Bff.ViewModels.Utility;
using TightWiki.Web.Engine.Library.Interfaces;
using TightWiki.Web.Engine.Utility;
using static TightWiki.Contracts.Constants;

namespace TightWiki.Web.Bff.Services
{
    public class AdminService(
        IConfigurationService configurationService,
        IEmojiService emojiService,
        IExceptionService exceptionService,
        IPageService pageService,
        IPageFileService pageFileService,
        ISpannedService spannedService,
        IStatisticsService statisticsService,
        IUsersService usersService,
        UserManager<IdentityUser> userManager,
        ITightEngine engine,
        IEngineConfigurationProvider configProvider,
        IEngineDataProvider dataProvider,
        ISessionState session,
        IStringLocalizer<AdminService> localizer)
        : IAdminBffService
    {
        #region Database

        public DatabaseViewModel GetDatabaseViewModel()
        {
            session.Page.Name = localizer.Localize("Database");

            var versions = spannedService.GetDatabaseVersions();
            var pageCounts = spannedService.GetDatabasePageCounts();
            var pageSizes = spannedService.GetDatabasePageSizes();

            var info = versions.Select(v =>
            {
                var pageCount = pageCounts.FirstOrDefault(o => o.Name == v.Name).PageCount;
                var pageSize = pageSizes.FirstOrDefault(o => o.Name == v.Name).PageSize;
                return new DatabaseInfo
                {
                    Name = v.Name,
                    Version = v.Version,
                    PageCount = pageCount,
                    PageSize = pageSize,
                    DatabaseSize = pageCount * pageSize
                };
            }).ToList();

            return new DatabaseViewModel { Info = info };
        }

        public IActionResult ExecuteDatabaseAction(DatabaseActionRequest request)
        {
            if (request.Confirm.UserSelection != true)
                return ConfirmNoRedirect(request.Confirm);

            var resultText = request.DatabaseAction switch
            {
                "Optimize" => spannedService.OptimizeDatabase(request.Database),
                "Vacuum" => spannedService.VacuumDatabase(request.Database),
                "Verify" => spannedService.IntegrityCheckDatabase(request.Database),
                _ => throw new InvalidOperationException($"Unknown database action: '{request.DatabaseAction}'")
            };

            var label = request.DatabaseAction switch
            {
                "Optimize" => $"Optimization complete. {resultText}",
                "Vacuum" => $"Vacuum complete. {resultText}",
                "Verify" => $"Verification complete. {resultText}",
                _ => resultText
            };

            return ConfirmSuccess(label, request.Confirm);
        }

        #endregion

        #region Metrics

        public MetricsViewModel GetMetricsViewModel()
        {
            session.Page.Name = localizer.Localize("Metrics");

            var version = string.Join('.', (Assembly.GetEntryAssembly()
                ?.GetName().Version?.ToString() ?? "0.0.0.0").Split('.').Take(3));

            return new MetricsViewModel
            {
                Metrics = configurationService.GetDatabaseStatistics(),
                ApplicationVersion = version
            };
        }

        public IActionResult PurgeCompilationStatistics(ConfirmActionViewModel model)
        {
            if (model.UserSelection != true)
                return ConfirmNoRedirect(model);

            statisticsService.PurgeCompilationStatistics();
            return ConfirmSuccess(localizer.Localize("Compilation statistics purged."), model);
        }

        public IActionResult PurgeMemoryCache(ConfirmActionViewModel model)
        {
            if (model.UserSelection != true)
                return ConfirmNoRedirect(model);

            WikiCache.Clear();
            return ConfirmSuccess(localizer.Localize("Memory cache purged."), model);
        }

        #endregion

        #region Compilation Statistics

        public PageCompilationStatisticsViewModel GetCompilationStatisticsViewModel(PagedRequest request)
        {
            session.Page.Name = localizer.Localize("Compilations Statistics");

            var stats = statisticsService.GetCompilationStatisticsPaged(request.Page, request.OrderBy, request.OrderByDirection);

            foreach (var o in stats)
                o.LatestBuild = session.LocalizeDateTime(o.LatestBuild);

            return new PageCompilationStatisticsViewModel
            {
                Statistics = stats,
                PaginationPageCount = stats.FirstOrDefault()?.PaginationPageCount ?? 0
            };
        }

        #endregion

        #region Moderate

        public PageModerateViewModel GetModerateViewModel(ModerateRequest request)
        {
            session.Page.Name = localizer.Localize("Page Moderation");

            if (request.Instruction != null)
            {
                var pages = pageService.GetAllPagesByInstructionPaged(request.Page, request.Instruction);

                pages.ForEach(o =>
                {
                    o.CreatedDate = session.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = session.LocalizeDateTime(o.ModifiedDate);
                });

                return new PageModerateViewModel
                {
                    Pages = pages,
                    Instruction = request.Instruction,
                    Instructions = typeof(WikiInstruction).GetProperties().Select(o => o.Name).ToList(),
                    PaginationPageCount = pages.FirstOrDefault()?.PaginationPageCount ?? 0
                };
            }

            return new PageModerateViewModel
            {
                Pages = new(),
                Instruction = string.Empty,
                Instructions = typeof(WikiInstruction).GetProperties().Select(o => o.Name).ToList()
            };
        }

        #endregion

        #region Missing Pages

        public MissingPagesViewModel GetMissingPagesViewModel(PagedRequest request)
        {
            session.Page.Name = localizer.Localize("Missing Pages");

            var pages = pageService.GetMissingPagesPaged(request.Page, request.OrderBy, request.OrderByDirection);
            return new MissingPagesViewModel
            {
                Pages = pages,
                PaginationPageCount = pages.FirstOrDefault()?.PaginationPageCount ?? 0
            };
        }

        #endregion

        #region Namespaces

        public NamespacesViewModel GetNamespacesViewModel(PagedRequest request)
        {
            session.Page.Name = localizer.Localize("Namespaces");

            var namespaces = pageService.GetAllNamespacesPaged(request.Page, request.OrderBy, request.OrderByDirection);
            return new NamespacesViewModel
            {
                Namespaces = namespaces,
                PaginationPageCount = namespaces.FirstOrDefault()?.PaginationPageCount ?? 0
            };
        }

        public NamespaceViewModel GetNamespaceViewModel(NamespaceRequest request)
        {
            session.Page.Name = localizer.Localize("Namespace");

            var pages = pageService.GetAllNamespacePagesPaged(request.Page, request.NamespaceName ?? string.Empty, request.OrderBy, request.OrderByDirection);

            pages.ForEach(o =>
            {
                o.CreatedDate = session.LocalizeDateTime(o.CreatedDate);
                o.ModifiedDate = session.LocalizeDateTime(o.ModifiedDate);
            });

            return new NamespaceViewModel
            {
                Pages = pages,
                Namespace = request.NamespaceName ?? string.Empty,
                PaginationPageCount = pages.FirstOrDefault()?.PaginationPageCount ?? 0
            };
        }

        #endregion

        #region Pages

        public PagesViewModel GetPagesViewModel(SearchPagedRequest request)
        {
            session.Page.Name = localizer.Localize("Pages");

            var pages = pageService.GetAllPagesPaged(request.Page, request.OrderBy, request.OrderByDirection, Utility.SplitToTokens(request.SearchString));

            pages.ForEach(o =>
            {
                o.CreatedDate = session.LocalizeDateTime(o.CreatedDate);
                o.ModifiedDate = session.LocalizeDateTime(o.ModifiedDate);
            });

            return new PagesViewModel
            {
                Pages = pages,
                SearchString = request.SearchString,
                PaginationPageCount = pages.FirstOrDefault()?.PaginationPageCount ?? 0
            };
        }

        #endregion

        #region Revisions

        public IActionResult RevertPageRevision(ConfirmCanonicalRevisionRequest request)
        {
            if (request.Confirm.UserSelection != true)
                return ConfirmNoRedirect(request.Confirm);

            var pageNavigation = NamespaceNavigation.CleanAndValidate(request.GivenCanonical);
            var page = pageService.GetPageRevisionByNavigation(pageNavigation, request.Revision)
                ?? throw new InvalidOperationException("Page revision not found.");

            int currentPageRevision = pageService.GetCurrentPageRevision(page.Id);
            if (request.Revision >= currentPageRevision)
                throw new InvalidOperationException("You cannot revert to the current page revision.");

            var config = configProvider.GetEngineConfiguration();
            var enginePage = MapToEnginePage(page);
            TightWiki.Web.Engine.Helpers.UpsertPage(engine, config, dataProvider, enginePage, null);

            return ConfirmSuccess(localizer.Localize("The page has been reverted."), request.Confirm);
        }

        public DeletedPagesRevisionsViewModel GetDeletedPageRevisionsViewModel(int pageId, PagedRequest request)
        {
            var revisions = pageService.GetDeletedPageRevisionsByIdPaged(pageId, request.Page, request.OrderBy, request.OrderByDirection);
            var page = pageService.GetLimitedPageInfoByIdAndRevision(pageId);

            revisions.ForEach(o => o.DeletedDate = session.LocalizeDateTime(o.DeletedDate));

            var model = new DeletedPagesRevisionsViewModel
            {
                Revisions = revisions,
                PageId = pageId,
                PaginationPageCount = revisions.FirstOrDefault()?.PaginationPageCount ?? 0
            };

            if (page != null)
            {
                model.Name = page.Name;
                model.Namespace = page.Namespace;
                model.Navigation = page.Navigation;
            }

            return model;
        }

        public DeletedPageRevisionViewModel GetDeletedPageRevisionViewModel(int pageId, int revision)
        {
            var model = new DeletedPageRevisionViewModel();
            var page = pageService.GetDeletedPageRevisionById(pageId, revision);

            if (page != null)
            {
                var config = configProvider.GetEngineConfiguration();
                var state = engine.Transform(config, dataProvider, session, page);
                model.PageId = pageId;
                model.Revision = revision;
                model.Body = state.HtmlResult;
                model.DeletedDate = session.LocalizeDateTime(page.DeletedDate);
                model.DeletedByUserName = page.DeletedByUserName;
            }

            return model;
        }

        public PageRevisionsViewModel GetPageRevisionsViewModel(string givenCanonical, PagedRequest request)
        {
            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);
            var revisions = pageService.GetPageRevisionsInfoByNavigationPaged(pageNavigation, request.Page, request.OrderBy, request.OrderByDirection);

            foreach (var p in revisions)
            {
                var thisRev = pageService.GetPageRevisionByNavigation(p.Navigation, p.Revision);
                var prevRev = pageService.GetPageRevisionByNavigation(p.Navigation, p.Revision - 1);
                p.ChangeAnalysis = Differentiator.GetComparisonSummary(thisRev?.Body ?? "", prevRev?.Body ?? "");

                p.CreatedDate = session.LocalizeDateTime(p.CreatedDate);
                p.ModifiedDate = session.LocalizeDateTime(p.ModifiedDate);
            }

            if (revisions.Count > 0)
                session.SetPageId(revisions.First().PageId);

            return new PageRevisionsViewModel
            {
                Revisions = revisions,
                PaginationPageCount = revisions.FirstOrDefault()?.PaginationPageCount ?? 0
            };
        }

        public IActionResult DeletePageRevision(ConfirmCanonicalRevisionRequest request)
        {
            if (request.Confirm.UserSelection != true)
                return ConfirmNoRedirect(request.Confirm);

            var pageNavigation = NamespaceNavigation.CleanAndValidate(request.GivenCanonical);
            var page = pageService.GetPageInfoByNavigation(pageNavigation)
                ?? throw new InvalidOperationException("Page not found.");

            int revisionCount = pageService.GetPageRevisionCountByPageId(page.Id);
            if (revisionCount <= 1)
                throw new InvalidOperationException("You cannot delete the only existing revision of a page.");

            if (request.Revision >= page.Revision)
            {
                int previousRevision = pageService.GetPagePreviousRevision(page.Id, request.Revision);
                var previousPageRevision = pageService.GetPageRevisionByNavigation(pageNavigation, previousRevision)
                    ?? throw new InvalidOperationException("Previous page revision not found.");
                var config = configProvider.GetEngineConfiguration();
                var enginePage = MapToEnginePage(previousPageRevision);
                TightWiki.Web.Engine.Helpers.UpsertPage(engine, config, dataProvider, enginePage, null);
            }

            pageService.MovePageRevisionToDeletedById(page.Id, request.Revision, session.Profile.EnsureNotNull().UserId);

            return ConfirmSuccess(localizer.Localize("Page revision has been moved to the deletion queue."), request.Confirm);
        }

        #endregion

        #region Deleted Pages

        public DeletedPageViewModel GetDeletedPageViewModel(int pageId)
        {
            var model = new DeletedPageViewModel();
            var page = pageService.GetDeletedPageById(pageId);

            if (page != null)
            {
                var config = configProvider.GetEngineConfiguration();
                var state = engine.Transform(config, dataProvider, session, page);
                model.PageId = pageId;
                model.Body = state.HtmlResult;
                model.DeletedDate = session.LocalizeDateTime(page.ModifiedDate);
                model.DeletedByUserName = page.DeletedByUserName;
            }

            return model;
        }

        public DeletedPagesViewModel GetDeletedPagesViewModel(SearchPagedRequest request)
        {
            var pages = pageService.GetAllDeletedPagesPaged(request.Page, request.OrderBy, request.OrderByDirection, Utility.SplitToTokens(request.SearchString));
            return new DeletedPagesViewModel
            {
                Pages = pages,
                SearchString = request.SearchString,
                PaginationPageCount = pages.FirstOrDefault()?.PaginationPageCount ?? 0
            };
        }

        public IActionResult RebuildAllPages(ConfirmActionViewModel model)
        {
            if (model.UserSelection != true)
                return ConfirmNoRedirect(model);

            var config = configProvider.GetEngineConfiguration();
            foreach (var page in pageService.GetAllPages())
            {
                var enginePage = MapToEnginePage(page);
                TightWiki.Web.Engine.Helpers.RefreshPageMetadata(engine, config, dataProvider, enginePage, session);
            }

            return ConfirmSuccess(localizer.Localize("All pages have been rebuilt."), model);
        }

        public IActionResult PreCacheAllPages(ConfirmActionViewModel model)
        {
            if (model.UserSelection != true)
                return ConfirmNoRedirect(model);

            var config = configProvider.GetEngineConfiguration();
            var pool = new NTDLS.DelegateThreadPooling.DelegateThreadPool();
            var workload = pool.CreateChildPool();

            foreach (var page in pageService.GetAllPages())
            {
                workload.Enqueue(() =>
                {
                    var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [page.Navigation, page.Revision, string.Empty]);
                    if (WikiCache.Contains(cacheKey) == false)
                    {
                        var state = engine.Transform(config, dataProvider, session, page, page.Revision);
                        if (state.ProcessingInstructions.Contains(WikiInstruction.NoCache) == false)
                            WikiCache.Put(cacheKey, state.HtmlResult);
                    }
                });
            }

            workload.WaitForCompletion();
            return ConfirmSuccess(localizer.Localize("All pages have been cached."), model);
        }

        public IActionResult TruncatePageRevisions(ConfirmActionViewModel model)
        {
            if (model.UserSelection != true)
                return ConfirmNoRedirect(model);

            pageService.TruncateAllPageRevisions("YES");
            WikiCache.Clear();
            return ConfirmSuccess(localizer.Localize("All page revisions have been truncated."), model);
        }

        public IActionResult PurgeDeletedPageRevisions(ConfirmPageRequest request)
        {
            if (request.Confirm.UserSelection != true)
                return ConfirmNoRedirect(request.Confirm);

            pageService.PurgeDeletedPageRevisionsByPageId(request.PageId);
            return ConfirmSuccess(localizer.Localize("The page deletion queue has been purged."), request.Confirm);
        }

        public IActionResult PurgeDeletedPageRevision(ConfirmPageRevisionRequest request)
        {
            if (request.Confirm.UserSelection != true)
                return ConfirmNoRedirect(request.Confirm);

            pageService.PurgeDeletedPageRevisionByPageIdAndRevision(request.PageId, request.Revision);
            return ConfirmSuccess(localizer.Localize("The page revision has been purged from the deletion queue."), request.Confirm);
        }

        public IActionResult RestoreDeletedPageRevision(ConfirmPageRevisionRequest request)
        {
            if (request.Confirm.UserSelection != true)
                return ConfirmNoRedirect(request.Confirm);

            pageService.RestoreDeletedPageRevisionByPageIdAndRevision(request.PageId, request.Revision);
            return ConfirmSuccess(localizer.Localize("The page revision has been restored."), request.Confirm);
        }

        public IActionResult PurgeDeletedPages(ConfirmActionViewModel model)
        {
            if (model.UserSelection != true)
                return ConfirmNoRedirect(model);

            pageService.PurgeDeletedPages();
            return ConfirmSuccess(localizer.Localize("The page deletion queue has been purged."), model);
        }

        public IActionResult PurgeDeletedPage(ConfirmPageRequest request)
        {
            if (request.Confirm.UserSelection != true)
                return ConfirmNoRedirect(request.Confirm);

            pageService.PurgeDeletedPageByPageId(request.PageId);
            return ConfirmSuccess(localizer.Localize("The page has been purged from the deletion queue."), request.Confirm);
        }

        public IActionResult DeletePage(ConfirmPageRequest request)
        {
            if (request.Confirm.UserSelection != true)
                return ConfirmNoRedirect(request.Confirm);

            pageService.MovePageToDeletedById(request.PageId, session.Profile.EnsureNotNull().UserId);
            return ConfirmSuccess(localizer.Localize("The page has been moved to the deletion queue."), request.Confirm);
        }

        public IActionResult RestoreDeletedPage(ConfirmPageRequest request)
        {
            if (request.Confirm.UserSelection != true)
                return ConfirmNoRedirect(request.Confirm);

            pageService.RestoreDeletedPageByPageId(request.PageId);
            var page = pageService.GetLatestPageRevisionById(request.PageId);
            if (page != null)
            {
                var config = configProvider.GetEngineConfiguration();
                var enginePage = MapToEnginePage(page);
                TightWiki.Web.Engine.Helpers.RefreshPageMetadata(engine, config, dataProvider, enginePage, session);
            }

            return ConfirmSuccess(localizer.Localize("The page has restored."), request.Confirm);
        }

        #endregion

        #region Files

        public OrphanedPageAttachmentsViewModel GetOrphanedPageAttachmentsViewModel(PagedRequest request)
        {
            session.Page.Name = localizer.Localize("Orphaned Page Attachments");

            var files = pageFileService.GetOrphanedPageAttachmentsPaged(request.Page, request.OrderBy, request.OrderByDirection);
            return new OrphanedPageAttachmentsViewModel
            {
                Files = files,
                PaginationPageCount = files.FirstOrDefault()?.PaginationPageCount ?? 0
            };
        }

        public IActionResult PurgeOrphanedAttachments(ConfirmActionViewModel model)
        {
            if (model.UserSelection != true)
                return ConfirmNoRedirect(model);

            pageFileService.PurgeOrphanedPageAttachments();
            return ConfirmSuccess(localizer.Localize("All orphaned page attachments have been purged."), model);
        }

        public IActionResult PurgeOrphanedAttachment(PurgeOrphanedAttachmentRequest request)
        {
            if (request.Confirm.UserSelection != true)
                return ConfirmNoRedirect(request.Confirm);

            pageFileService.PurgeOrphanedPageAttachment(request.PageFileId, request.Revision);
            return ConfirmSuccess(localizer.Localize("The pages orphaned attachments have been purged."), request.Confirm);
        }

        #endregion

        #region Menu Items

        public MenuItemsViewModel GetMenuItemsViewModel(PagedRequest request)
        {
            return new MenuItemsViewModel
            {
                Items = configurationService.GetAllMenuItems(request.OrderBy, request.OrderByDirection)
            };
        }

        public MenuItemViewModel GetMenuItemViewModel(int? id)
        {
            session.Page.Name = localizer.Localize("Menu Item");

            if (id != null)
                return MenuItemViewModel.FromDataModel(configurationService.GetMenuItemById((int)id));

            return new MenuItemViewModel { Link = "/" };
        }

        public IActionResult SaveMenuItem(SaveMenuItemRequest request)
        {
            var model = request.Model;

            if (configurationService.GetAllMenuItems()
                .Any(o => o.Name.Equals(model.Name, StringComparison.InvariantCultureIgnoreCase) && o.Id != model.Id))
            {
                throw new InvalidOperationException(
                    localizer.Localize("The menu name '{0}' is already in use.", model.Name));
            }

            if (request.Id.DefaultWhenNull(0) == 0)
            {
                model.Id = configurationService.CreateMenuItem(model.ToDataModel());
                return NotifySuccess(localizer.Localize("The menu item has been created."), $"/Admin/MenuItem/{model.Id}");
            }

            configurationService.UpdateMenuItem(model.ToDataModel());
            return NotifySuccess(localizer.Localize("The menu item has been saved."), $"/Admin/MenuItem/{model.Id}");
        }

        public IActionResult DeleteMenuItem(DeleteMenuItemRequest request)
        {
            if (request.Confirm.UserSelection != true)
                return ConfirmNoRedirect(request.Confirm);

            configurationService.DeleteMenuItem(request.Id);
            return ConfirmSuccess(localizer.Localize("The specified menu item has been deleted."), request.Confirm);
        }

        #endregion

        #region Config

        public ConfigurationViewModel GetConfigViewModel()
        {
            return new ConfigurationViewModel
            {
                Themes = configurationService.GetAllThemes(),
                Roles = usersService.GetAllRoles(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Nest = configurationService.GetConfigurationNest()
            };
        }

        public IActionResult SaveConfiguration(ConfigurationViewModel model, Func<string, string> getFormValue)
        {
            var flatConfig = configurationService.GetFlatConfiguration();

            foreach (var fc in flatConfig)
            {
                var parent = model.Nest.Single(o => o.Name == fc.GroupName);
                var child = parent.Entries.Single(o => o.Name == fc.EntryName);
                var value = getFormValue($"{fc.GroupId}:{fc.EntryId}");

                child.Value = value;

                if (fc.IsRequired && string.IsNullOrEmpty(value))
                    throw new InvalidOperationException($"{fc.GroupName} : {fc.EntryName} is required.");

                if ($"{fc.GroupName}:{fc.EntryName}" == "Customization:Theme")
                {
                    GlobalConfiguration.SystemTheme = configurationService.GetAllThemes().Single(o => o.Name == value);
                }

                if (fc.IsEncrypted)
                    value = Helpers.EncryptString(Helpers.MachineKey, value);

                configurationService.SaveConfigurationValue(fc.GroupName, fc.EntryName, value);
            }

            WikiCache.ClearCategory(WikiCache.Category.Configuration);

            if (string.IsNullOrEmpty(session.Profile?.Theme))
                session.UserTheme = GlobalConfiguration.SystemTheme;

            return NotifySuccess(localizer.Localize("The configuration has been saved successfully!"), "/Admin/Config");
        }

        #endregion

        #region Emojis

        public EmojisViewModel GetEmojisViewModel(SearchPagedRequest request)
        {
            session.Page.Name = localizer.Localize("Emojis");

            var emojis = emojiService.GetAllEmojisPaged(request.Page, request.OrderBy, request.OrderByDirection, Utility.SplitToTokens(request.SearchString));
            return new EmojisViewModel
            {
                Emojis = emojis,
                SearchString = request.SearchString,
                PaginationPageCount = emojis.FirstOrDefault()?.PaginationPageCount ?? 0
            };
        }

        public EmojiViewModel GetEmojiViewModel(string name)
        {
            var emoji = emojiService.GetEmojiByName(name);
            return new EmojiViewModel
            {
                Emoji = emoji ?? new Emoji(),
                Categories = string.Join(",", emojiService.GetEmojiCategoriesByName(name).Select(o => o.Category).ToList()),
                OriginalName = emoji?.Name ?? string.Empty
            };
        }

        public IActionResult SaveEmoji(SaveEmojiUploadRequest request)
        {
            var model = request.Model;

            bool nameChanged = !model.OriginalName.Equals(model.Emoji.Name, StringComparison.InvariantCultureIgnoreCase);
            if (nameChanged && emojiService.GetEmojiByName(model.Emoji.Name.ToLowerInvariant()) != null)
                throw new InvalidOperationException(localizer.Localize("Emoji name is already in use."));

            var emoji = new UpsertEmoji
            {
                Id = model.Emoji.Id,
                Name = model.Emoji.Name.ToLowerInvariant(),
                Categories = Utility.SplitToTokens($"{model.Categories} {model.Emoji.Name} {Text.SeparateCamelCase(model.Emoji.Name)}")
            };

            ProcessEmojiImage(request.ImageFile, emoji);

            emojiService.UpsertEmoji(emoji);

            if (nameChanged)
                return NotifySuccess(localizer.Localize("The emoji has been saved."), $"/Admin/Emoji/{Navigation.Clean(emoji.Name)}");

            return NotifySuccess(localizer.Localize("The emoji has been saved successfully!"), $"/Admin/Emoji/{Navigation.Clean(model.OriginalName)}");
        }

        public IActionResult CreateEmoji(CreateEmojiUploadRequest request)
        {
            var model = request.Model;

            if (string.IsNullOrEmpty(model.OriginalName) || !model.OriginalName.Equals(model.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                if (emojiService.GetEmojiByName(model.Name.ToLowerInvariant()) != null)
                    throw new InvalidOperationException(localizer.Localize("Emoji name is already in use."));
            }

            var emoji = new UpsertEmoji
            {
                Id = model.Id,
                Name = model.Name.ToLowerInvariant(),
                Categories = Utility.SplitToTokens($"{model.Categories} {model.Name} {Text.SeparateCamelCase(model.Name)}")
            };

            ProcessEmojiImage(request.ImageFile, emoji);

            emojiService.UpsertEmoji(emoji);
            return NotifySuccess(localizer.Localize("The emoji has been created."), $"/Admin/Emoji/{Navigation.Clean(emoji.Name)}");
        }

        private static void ProcessEmojiImage(Microsoft.AspNetCore.Http.IFormFile? file, UpsertEmoji emoji)
        {
            if (file == null || file.Length <= 0)
                return;

            if (file.Length > GlobalConfiguration.MaxEmojiFileSize)
                throw new InvalidOperationException("Could not save the attached image, too large.");

            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new BinaryReader(stream);
                var imageBytes = reader.ReadBytes((int)file.Length);

                _ = SixLabors.ImageSharp.Image.Load(new MemoryStream(imageBytes));
                emoji.ImageData = imageBytes;
                emoji.MimeType = file.ContentType;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch
            {
                throw new InvalidOperationException("Could not save the attached image.");
            }
        }

        public IActionResult DeleteEmoji(DeleteEmojiRequest request)
        {
            if (request.Confirm.UserSelection != true)
                return ConfirmNoRedirect(request.Confirm);

            var emoji = emojiService.GetEmojiByName(request.Name);
            if (emoji != null)
                emojiService.DeleteById(emoji.Id);

            return ConfirmSuccess(localizer.Localize("The specified emoji has been deleted."), request.Confirm);
        }

        #endregion

        #region Exceptions

        public ExceptionsViewModel GetExceptionsViewModel(PagedRequest request)
        {
            session.Page.Name = localizer.Localize("Exceptions");

            var exceptions = exceptionService.GetAllExceptionsPaged(request.Page, request.OrderBy, request.OrderByDirection);
            return new ExceptionsViewModel
            {
                Exceptions = exceptions,
                PaginationPageCount = exceptions.FirstOrDefault()?.PaginationPageCount ?? 0
            };
        }

        public ExceptionViewModel GetExceptionViewModel(int id)
        {
            session.Page.Name = localizer.Localize("Exception");

            return new ExceptionViewModel
            {
                Exception = exceptionService.GetExceptionById(id)
            };
        }

        public IActionResult PurgeExceptions(ConfirmActionViewModel model)
        {
            if (model.UserSelection != true)
                return ConfirmNoRedirect(model);

            exceptionService.PurgeExceptions();
            return ConfirmSuccess(localizer.Localize("All exceptions have been purged."), model);
        }

        #endregion

        #region LDAP

        public async Task<IActionResult> TestLdapAsync(LdapTestRequest request)
        {
            var ldapConfig = configurationService.GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.LDAPAuthentication);

            if (GlobalConfiguration.EnableLDAPAuthentication == false)
                return new BadRequestObjectResult(ApiResponse.Fail("LDAP authentication is not enabled."));

            if (LDAPUtility.LdapCredentialChallenge(ldapConfig, StaticLocalizer.Localizer,
                request.Username, request.Password, out var samAccountName, out var objectGuid))
            {
                if (objectGuid == null || objectGuid == Guid.Empty)
                    return new JsonResult(new LdapTestResult { Error = "LDAP challenge succeeded, but the user does not have an objectGUID attribute." });

                var loginInfo = new UserLoginInfo("LDAP", objectGuid.Value.ToString(), "Active Directory");
                var foundUser = await userManager.FindByLoginAsync(loginInfo.LoginProvider, loginInfo.ProviderKey);

                if (foundUser == null)
                    return new JsonResult(new LdapTestResult { Ok = true, Message = "LDAP challenge succeeded (un-provisioned account).", DistinguishedName = samAccountName });

                if (usersService.TryGetBasicProfileByUserId(Guid.Parse(foundUser.Id), out _))
                    return new JsonResult(new LdapTestResult { Ok = true, Message = "LDAP challenge succeeded (fully provisioned account).", DistinguishedName = samAccountName });

                return new JsonResult(new LdapTestResult { Ok = true, Message = "LDAP challenge succeeded (partially provisioned account).", DistinguishedName = samAccountName });
            }

            return new JsonResult(new LdapTestResult { Error = "LDAP challenge failed." });
        }

        #endregion

        #region Private Helpers

        private static RedirectResult NotifySuccess(string message, string redirectUrl)
            => new($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{redirectUrl}")}&RedirectTimeout=5");

        private static RedirectResult ConfirmSuccess(string message, ConfirmActionViewModel model)
            => new($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{model.YesRedirectURL}")}&RedirectTimeout=5");

        private static RedirectResult ConfirmNoRedirect(ConfirmActionViewModel model)
            => new($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");

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
