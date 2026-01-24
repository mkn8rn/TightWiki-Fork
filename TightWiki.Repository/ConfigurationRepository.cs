using SixLabors.ImageSharp;
using System.Data;
using System.Diagnostics;
using System.Runtime.Caching;
using DAL;
using DalConfigurationEntry = DAL.Models.ConfigurationEntry;
using DalConfigurationGroup = DAL.Models.ConfigurationGroup;
using DalMenuItem = DAL.Models.MenuItem;
using DalWikiDatabaseStatistics = DAL.Models.WikiDatabaseStatistics;
using DalTheme = DAL.Models.Theme;
using Microsoft.EntityFrameworkCore;
using NTDLS.Helpers;
using TightWiki.Caching;
using TightWiki.Library;
using TightWiki.Models;
using ApiConfigurationEntries = TightWiki.Models.DataModels.ConfigurationEntries;
using ApiConfigurationEntry = TightWiki.Models.DataModels.ConfigurationEntry;
using ApiConfigurationFlat = TightWiki.Models.DataModels.ConfigurationFlat;
using ApiConfigurationNest = TightWiki.Models.DataModels.ConfigurationNest;
using ApiMenuItem = TightWiki.Models.DataModels.MenuItem;
using ApiWikiDatabaseStatistics = TightWiki.Models.DataModels.WikiDatabaseStatistics;
using ApiImageCacheItem = TightWiki.Models.DataModels.ImageCacheItem;

namespace TightWiki.Repository
{
    public interface IConfigurationRepository
    {
        ApiConfigurationEntries GetConfigurationEntryValuesByGroupName(string groupName);
        List<Theme> GetAllThemes();
        string? GetConfigurationEntryValuesByGroupNameAndEntryName(string groupName, string entryName);
        void SaveConfigurationEntryValueByGroupAndEntry(string groupName, string entryName, string value);
        List<ApiConfigurationFlat> GetFlatConfiguration();
        List<ApiConfigurationNest> GetConfigurationNest();

        List<ApiMenuItem> GetAllMenuItems(string? orderBy = null, string? orderByDirection = null);
        ApiMenuItem GetMenuItemById(int id);
        void DeleteMenuItemById(int id);
        int UpdateMenuItemById(ApiMenuItem menuItem);
        int InsertMenuItem(ApiMenuItem menuItem);

        bool GetCryptoCheck();
        void SetCryptoCheck();
    }

    public static class ConfigurationRepository
    {
        private static IServiceProvider? _serviceProvider;
        public static void UseServiceProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        private static IConfigurationRepository Repo =>
            _serviceProvider?.GetService(typeof(IConfigurationRepository)) as IConfigurationRepository
            ?? throw new InvalidOperationException("IConfigurationRepository is not configured.");

        public static ApiConfigurationEntries GetConfigurationEntryValuesByGroupName(string groupName)
            => Repo.GetConfigurationEntryValuesByGroupName(groupName);

        public static List<Theme> GetAllThemes()
            => Repo.GetAllThemes();

        public static ApiWikiDatabaseStatistics GetWikiDatabaseMetrics()
        {
            return ManagedDataStorage.Config.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                using var pages_db = o.Attach("pages.db", "pages_db");

                var result = o.QuerySingle<ApiWikiDatabaseStatistics>("GetWikiDatabaseStatistics.sql");
                result.Exceptions = ExceptionRepository.GetExceptionCount();

                return result;
            });
        }

        /// <summary>
        /// Determines if this is the first time the wiki has run. Returns true if it is the first time.
        /// </summary>
        public static bool IsFirstRun()
        {
            bool isEncryptionValid = GetCryptoCheck();
            if (isEncryptionValid == false)
            {
                SetCryptoCheck();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Reads an encrypted value from the database so we can determine if encryption is setup.
        /// If the value is missing then we are NOT setup.
        /// If the value is present but we cant decrypt it, then we are NOT setup.
        /// If the value is present and we can decrypt it, then we are setup and good to go!
        /// </summary>
        public static bool GetCryptoCheck()
            => Repo.GetCryptoCheck();

        /// <summary>
        /// Writes an encrypted value to the database so we can test at a later time to ensure that encryption is setup.
        /// </summary>
        public static void SetCryptoCheck()
            => Repo.SetCryptoCheck();

        public static void SaveConfigurationEntryValueByGroupAndEntry(string groupName, string entryName, string value)
            => Repo.SaveConfigurationEntryValueByGroupAndEntry(groupName, entryName, value);

        public static List<ApiConfigurationNest> GetConfigurationNest()
            => Repo.GetConfigurationNest();

        public static List<ApiConfigurationFlat> GetFlatConfiguration()
            => Repo.GetFlatConfiguration();

        public static string? GetConfigurationEntryValuesByGroupNameAndEntryName(string groupName, string entryName)
            => Repo.GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName);

        public static T? Get<T>(string groupName, string entryName)
        {
            var value = GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName);
            return Converters.ConvertTo<T>(value.EnsureNotNull());
        }

        public static T? Get<T>(string groupName, string entryName, T defaultValue)
        {
            var value = GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName);

            if (value == null)
            {
                return defaultValue;
            }

            return Converters.ConvertTo<T>(value);
        }

        #region Menu Items.

        public static List<ApiMenuItem> GetAllMenuItems(string? orderBy = null, string? orderByDirection = null)
            => Repo.GetAllMenuItems(orderBy, orderByDirection);

        public static ApiMenuItem GetMenuItemById(int id)
            => Repo.GetMenuItemById(id);

        public static void DeleteMenuItemById(int id)
            => Repo.DeleteMenuItemById(id);

        public static int UpdateMenuItemById(ApiMenuItem menuItem)
            => Repo.UpdateMenuItemById(menuItem);

        public static int InsertMenuItem(ApiMenuItem menuItem)
            => Repo.InsertMenuItem(menuItem);

        #endregion

        public static void ReloadEmojis()
        {
            WikiCache.ClearCategory(WikiCache.Category.Emoji);
            GlobalConfiguration.Emojis = EmojiRepository.GetAllEmojis();

            if (GlobalConfiguration.PreLoadAnimatedEmojis)
            {
                new Thread(() =>
                {
                    var parallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount / 2 < 2 ? 2 : Environment.ProcessorCount / 2
                    };

                    Parallel.ForEach(GlobalConfiguration.Emojis, parallelOptions, emoji =>
                    {
                        if (emoji.MimeType.Equals("image/gif", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var imageCacheKey = WikiCacheKey.Build(WikiCache.Category.Emoji, [emoji.Shortcut]);
                            emoji.ImageData = EmojiRepository.GetEmojiByName(emoji.Name)?.ImageData;

                            if (emoji.ImageData != null)
                            {
                                var scaledImageCacheKey = WikiCacheKey.Build(WikiCache.Category.Emoji, [emoji.Shortcut, "100"]);
                                var decompressedImageBytes = Utility.Decompress(emoji.ImageData);
                                var img = Image.Load(new MemoryStream(decompressedImageBytes));

                                int customScalePercent = 100;

                                var (Width, Height) = Utility.ScaleToMaxOf(img.Width, img.Height, GlobalConfiguration.DefaultEmojiHeight);

                                //Adjust to any specified scaling.
                                Height = (int)(Height * (customScalePercent / 100.0));
                                Width = (int)(Width * (customScalePercent / 100.0));

                                //Adjusting by a ratio (and especially after applying additional scaling) may have caused one
                                //  dimension to become very small (or even negative). So here we will check the height and width
                                //  to ensure they are both at least n pixels and adjust both dimensions.
                                if (Height < 16)
                                {
                                    Height += 16 - Height;
                                    Width += 16 - Height;
                                }
                                if (Width < 16)
                                {
                                    Height += 16 - Width;
                                    Width += 16 - Width;
                                }

                                //These are hard to generate, so just keep it forever.
                                var resized = Images.ResizeGifImage(decompressedImageBytes, Width, Height);
                                var itemCache = new ApiImageCacheItem(resized, "image/gif");
                                WikiCache.Put(scaledImageCacheKey, itemCache, new CacheItemPolicy());
                            }
                        }
                    });
                }).Start();
            }
        }

        public static void ReloadEverything()
        {
            WikiCache.Clear();

            GlobalConfiguration.IsDebug = Debugger.IsAttached;

            var performanceConfig = GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Performance);
            GlobalConfiguration.PageCacheSeconds = performanceConfig.Value<int>("Page Cache Time (Seconds)");
            GlobalConfiguration.RecordCompilationMetrics = performanceConfig.Value<bool>("Record Compilation Metrics");
            GlobalConfiguration.CacheMemoryLimitMB = performanceConfig.Value<int>("Cache Memory Limit MB");

            WikiCache.Initialize(GlobalConfiguration.CacheMemoryLimitMB, GlobalConfiguration.PageCacheSeconds);

            var basicConfig = GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Basic);
            var customizationConfig = GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Customization);
            var htmlConfig = GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.HTMLLayout);
            var functionalityConfig = GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Functionality);
            var membershipConfig = GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Membership);
            var searchConfig = GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.Search);
            var filesAndAttachmentsConfig = GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.FilesAndAttachments);
            var ldapAuthentication = GetConfigurationEntryValuesByGroupName(Constants.ConfigurationGroup.LDAPAuthentication);

            GlobalConfiguration.EnableLDAPAuthentication = ldapAuthentication.Value("LDAP : Enable LDAP Authentication", false);

            GlobalConfiguration.Address = basicConfig?.Value<string>("Address") ?? string.Empty;
            GlobalConfiguration.Name = basicConfig?.Value<string>("Name") ?? string.Empty;
            GlobalConfiguration.Copyright = basicConfig?.Value<string>("Copyright") ?? string.Empty;

            var themeName = customizationConfig.Value("Theme", "Light");

            GlobalConfiguration.FixedMenuPosition = customizationConfig.Value("Fixed Header Menu Position", false);
            GlobalConfiguration.AllowSignup = membershipConfig.Value("Allow Signup", false);
            GlobalConfiguration.DefaultProfileRecentlyModifiedCount = performanceConfig.Value<int>("Default Profile Recently Modified Count");
            GlobalConfiguration.PreLoadAnimatedEmojis = performanceConfig.Value<bool>("Pre-Load Animated Emojis");
            GlobalConfiguration.SystemTheme = GetAllThemes().Single(o => o.Name == themeName);
            GlobalConfiguration.DefaultEmojiHeight = customizationConfig.Value<int>("Default Emoji Height");
            GlobalConfiguration.PaginationSize = customizationConfig.Value<int>("Pagination Size");

            GlobalConfiguration.DefaultTimeZone = customizationConfig?.Value<string>("Default TimeZone") ?? string.Empty;
            GlobalConfiguration.IncludeWikiDescriptionInMeta = functionalityConfig.Value<bool>("Include wiki Description in Meta");
            GlobalConfiguration.IncludeWikiTagsInMeta = functionalityConfig.Value<bool>("Include wiki Tags in Meta");
            GlobalConfiguration.EnablePageComments = functionalityConfig.Value<bool>("Enable Page Comments");
            GlobalConfiguration.EnablePublicProfiles = functionalityConfig.Value<bool>("Enable Public Profiles");
            GlobalConfiguration.ShowCommentsOnPageFooter = functionalityConfig.Value<bool>("Show Comments on Page Footer");
            GlobalConfiguration.ShowChangeSummaryWhenEditing = functionalityConfig.Value<bool>("Show Change Summary when Editing");
            GlobalConfiguration.RequireChangeSummaryWhenEditing = functionalityConfig.Value<bool>("Require Change Summary when Editing");
            GlobalConfiguration.ShowLastModifiedOnPageFooter = functionalityConfig.Value<bool>("Show Last Modified on Page Footer");
            GlobalConfiguration.IncludeSearchOnNavbar = searchConfig.Value<bool>("Include Search on Navbar");
            GlobalConfiguration.HTMLHeader = htmlConfig?.Value<string>("Header") ?? string.Empty;
            GlobalConfiguration.HTMLFooter = htmlConfig?.Value<string>("Footer") ?? string.Empty;
            GlobalConfiguration.HTMLPreBody = htmlConfig?.Value<string>("Pre-Body") ?? string.Empty;
            GlobalConfiguration.HTMLPostBody = htmlConfig?.Value<string>("Post-Body") ?? string.Empty;
            GlobalConfiguration.BrandImageSmall = customizationConfig?.Value<string>("Brand Image (Small)") ?? string.Empty;
            GlobalConfiguration.FooterBlurb = customizationConfig?.Value<string>("FooterBlurb") ?? string.Empty;
            GlobalConfiguration.MaxAvatarFileSize = filesAndAttachmentsConfig.Value<int>("Max Avatar File Size");
            GlobalConfiguration.MaxAttachmentFileSize = filesAndAttachmentsConfig.Value<int>("Max Attachment File Size");
            GlobalConfiguration.MaxEmojiFileSize = filesAndAttachmentsConfig.Value<int>("Max Emoji File Size");

            GlobalConfiguration.MenuItems = GetAllMenuItems();

            ReloadEmojis();
        }
    }

    public sealed class ConfigurationRepositoryEf : IConfigurationRepository
    {
        public WikiDbContext Db { get; }

        public ConfigurationRepositoryEf(WikiDbContext db)
        {
            Db = db;
        }

        public List<Theme> GetAllThemes()
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Configuration);

            return WikiCache.AddOrGet(cacheKey, () =>
            {
                var themes = Db.Themes.AsNoTracking()
                    .OrderBy(t => t.Name)
                    .Select(t => new Theme
                    {
                        Name = t.Name,
                        DelimitedFiles = t.DelimitedFiles,
                        ClassNavBar = t.ClassNavBar,
                        ClassNavLink = t.ClassNavLink,
                        ClassDropdown = t.ClassDropdown,
                        ClassBranding = t.ClassBranding,
                        EditorTheme = t.EditorTheme,
                        Files = new()
                    })
                    .ToList();

                foreach (var theme in themes)
                {
                    theme.Files = theme.DelimitedFiles.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                }

                return themes;
            }).EnsureNotNull();
        }

        public ApiConfigurationEntries GetConfigurationEntryValuesByGroupName(string groupName)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Configuration, [groupName]);

            return WikiCache.AddOrGet(cacheKey, () =>
            {
                var entries = Db.ConfigurationEntries.AsNoTracking()
                    .Where(e => Db.ConfigurationGroups.Any(g => g.Id == e.ConfigurationGroupId && g.Name == groupName))
                    .OrderBy(e => e.Name)
                    .Select(e => new ApiConfigurationEntry
                    {
                        Id = e.Id,
                        ConfigurationGroupId = e.ConfigurationGroupId,
                        Name = e.Name,
                        Value = e.Value,
                        DataTypeId = e.DataTypeId,
                        Description = e.Description,
                        IsEncrypted = e.IsEncrypted,
                        DataType = e.DataType
                    })
                    .ToList();

                foreach (var entry in entries)
                {
                    if (entry.IsEncrypted)
                    {
                        try
                        {
                            entry.Value = Security.Helpers.DecryptString(Security.Helpers.MachineKey, entry.Value);
                        }
                        catch
                        {
                            entry.Value = "";
                        }
                    }
                }

                return new ApiConfigurationEntries(entries);
            }).EnsureNotNull();
        }

        public string? GetConfigurationEntryValuesByGroupNameAndEntryName(string groupName, string entryName)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Configuration, [groupName, entryName]);

            return WikiCache.AddOrGet(cacheKey, () =>
            {
                var entry = Db.ConfigurationEntries.AsNoTracking()
                    .Where(e => e.Name == entryName)
                    .Where(e => Db.ConfigurationGroups.Any(g => g.Id == e.ConfigurationGroupId && g.Name == groupName))
                    .Select(e => new ApiConfigurationEntry
                    {
                        Id = e.Id,
                        ConfigurationGroupId = e.ConfigurationGroupId,
                        Name = e.Name,
                        Value = e.Value,
                        DataTypeId = e.DataTypeId,
                        Description = e.Description,
                        IsEncrypted = e.IsEncrypted,
                        DataType = e.DataType
                    })
                    .SingleOrDefault();

                if (entry?.IsEncrypted == true)
                {
                    try
                    {
                        entry.Value = Security.Helpers.DecryptString(Security.Helpers.MachineKey, entry.Value);
                    }
                    catch
                    {
                        entry.Value = "";
                    }
                }

                return entry?.Value;
            });
        }

        public void SaveConfigurationEntryValueByGroupAndEntry(string groupName, string entryName, string value)
        {
            var groupId = Db.ConfigurationGroups
                .Where(g => g.Name == groupName)
                .Select(g => g.Id)
                .Single();

            var entry = Db.ConfigurationEntries.Single(e => e.ConfigurationGroupId == groupId && e.Name == entryName);
            entry.Value = entry.IsEncrypted
                ? Security.Helpers.EncryptString(Security.Helpers.MachineKey, value)
                : value;

            Db.SaveChanges();

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            ConfigurationRepository.ReloadEverything();
        }

        public List<ApiConfigurationFlat> GetFlatConfiguration()
        {
            return (from g in Db.ConfigurationGroups.AsNoTracking()
                    join e in Db.ConfigurationEntries.AsNoTracking() on g.Id equals e.ConfigurationGroupId
                    select new ApiConfigurationFlat
                    {
                        GroupId = g.Id,
                        GroupName = g.Name,
                        GroupDescription = g.Description,
                        EntryId = e.Id,
                        EntryName = e.Name,
                        EntryValue = e.Value,
                        EntryDescription = e.Description,
                        IsEncrypted = e.IsEncrypted,
                        IsRequired = false,
                        DataType = e.DataType
                    })
                .ToList();
        }

        public List<ApiConfigurationNest> GetConfigurationNest()
        {
            var flatConfig = GetFlatConfiguration();
            var result = new List<ApiConfigurationNest>();

            var groups = flatConfig.GroupBy(o => o.GroupId).ToList();
            foreach (var group in groups)
            {
                var nest = new ApiConfigurationNest
                {
                    Id = group.Key,
                    Name = group.Select(o => o.GroupName).First(),
                    Description = group.Select(o => o.GroupDescription).First()
                };

                foreach (var value in group.OrderBy(o => o.EntryName))
                {
                    string entryValue;
                    if (value.IsEncrypted)
                    {
                        try
                        {
                            entryValue = Security.Helpers.DecryptString(Security.Helpers.MachineKey, value.EntryValue);
                        }
                        catch
                        {
                            entryValue = "";
                        }
                    }
                    else
                    {
                        entryValue = value.EntryValue;
                    }

                    nest.Entries.Add(new ApiConfigurationEntry
                    {
                        Id = value.EntryId,
                        Value = entryValue,
                        Description = value.EntryDescription,
                        Name = value.EntryName,
                        DataType = value.DataType.ToLowerInvariant(),
                        IsEncrypted = value.IsEncrypted,
                        ConfigurationGroupId = group.Key,
                    });
                }
                result.Add(nest);
            }

            return result;
        }

        public List<ApiMenuItem> GetAllMenuItems(string? orderBy = null, string? orderByDirection = null)
        {
            var asc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);

            IQueryable<DalMenuItem> query = Db.MenuItems.AsNoTracking();

            query = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "name" => asc ? query.OrderBy(m => m.Name) : query.OrderByDescending(m => m.Name),
                "link" => asc ? query.OrderBy(m => m.Link) : query.OrderByDescending(m => m.Link),
                "ordinal" => asc ? query.OrderBy(m => m.Ordinal) : query.OrderByDescending(m => m.Ordinal),
                _ => query.OrderBy(m => m.Ordinal)
            };

            return query.Select(m => new ApiMenuItem
            {
                Id = m.Id,
                Name = m.Name,
                Link = m.Link,
                Ordinal = m.Ordinal
            }).ToList();
        }

        public ApiMenuItem GetMenuItemById(int id)
        {
            var item = Db.MenuItems.AsNoTracking().Single(m => m.Id == id);
            return new ApiMenuItem { Id = item.Id, Name = item.Name, Link = item.Link, Ordinal = item.Ordinal };
        }

        public void DeleteMenuItemById(int id)
        {
            var entity = Db.MenuItems.SingleOrDefault(m => m.Id == id);
            if (entity == null)
            {
                return;
            }

            Db.MenuItems.Remove(entity);
            Db.SaveChanges();

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            GlobalConfiguration.MenuItems = ConfigurationRepository.GetAllMenuItems();
        }

        public int UpdateMenuItemById(ApiMenuItem menuItem)
        {
            var entity = Db.MenuItems.Single(m => m.Id == menuItem.Id);
            entity.Name = menuItem.Name;
            entity.Link = menuItem.Link;
            entity.Ordinal = menuItem.Ordinal;

            Db.SaveChanges();

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            GlobalConfiguration.MenuItems = ConfigurationRepository.GetAllMenuItems();

            return entity.Id;
        }

        public int InsertMenuItem(ApiMenuItem menuItem)
        {
            var entity = new DalMenuItem
            {
                Name = menuItem.Name,
                Link = menuItem.Link,
                Ordinal = menuItem.Ordinal
            };

            Db.MenuItems.Add(entity);
            Db.SaveChanges();

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            GlobalConfiguration.MenuItems = ConfigurationRepository.GetAllMenuItems();

            return entity.Id;
        }

        public bool GetCryptoCheck()
        {
            var value = Db.ConfigurationEntries.AsNoTracking()
                .Where(e => e.Name == "CryptoCheck")
                .Select(e => e.Value)
                .FirstOrDefault() ?? string.Empty;

            try
            {
                value = Security.Helpers.DecryptString(Security.Helpers.MachineKey, value);
                return value == Constants.CRYPTOCHECK;
            }
            catch
            {
                return false;
            }
        }

        public void SetCryptoCheck()
        {
            var encrypted = Security.Helpers.EncryptString(Security.Helpers.MachineKey, Constants.CRYPTOCHECK);

            var entry = Db.ConfigurationEntries.SingleOrDefault(e => e.Name == "CryptoCheck");
            if (entry == null)
            {
                entry = new DAL.Models.ConfigurationEntry
                {
                    Name = "CryptoCheck",
                    Value = encrypted,
                    IsEncrypted = true,
                    ConfigurationGroupId = 1, // Basic configuration group
                    DataType = "string",
                    Description = "Internal encryption validation key"
                };
                Db.ConfigurationEntries.Add(entry);
            }
            else
            {
                entry.Value = encrypted;
                entry.IsEncrypted = true;
            }

            Db.SaveChanges();
        }
    }
}
