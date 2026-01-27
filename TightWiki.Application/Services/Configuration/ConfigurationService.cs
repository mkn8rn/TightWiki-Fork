using TightWiki.Contracts;
using TightWiki.Contracts.DataModels;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using NTDLS.Helpers;
using System.Diagnostics;
using TightWiki.Utils.Caching;
using TightWiki.Utils;
using TightWiki.Utils.Security;

namespace BLL.Services.Configuration
{
    /// <summary>
    /// Business logic service for configuration operations.
    /// </summary>
    public sealed class ConfigurationService : IConfigurationService
    {
        private readonly WikiDbContext _db;

        public ConfigurationService(WikiDbContext db)
        {
            _db = db;
        }

        #region Configuration Entries

        public ConfigurationEntries GetConfigurationEntriesByGroupName(string groupName)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Configuration, [groupName]);

            return WikiCache.AddOrGet(cacheKey, () =>
            {
                var entries = _db.ConfigurationEntries.AsNoTracking()
                    .Where(e => _db.ConfigurationGroups.Any(g => g.Id == e.ConfigurationGroupId && g.Name == groupName))
                    .OrderBy(e => e.Name)
                    .Select(e => new ConfigurationEntry
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
                            entry.Value = Helpers.DecryptString(Helpers.MachineKey, entry.Value);
                        }
                        catch
                        {
                            entry.Value = "";
                        }
                    }
                }

                return new ConfigurationEntries(entries);
            }).EnsureNotNull();
        }

        public string? GetConfigurationValue(string groupName, string entryName)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Configuration, [groupName, entryName]);

            return WikiCache.AddOrGet(cacheKey, () =>
            {
                var entry = _db.ConfigurationEntries.AsNoTracking()
                    .Where(e => e.Name == entryName)
                    .Where(e => _db.ConfigurationGroups.Any(g => g.Id == e.ConfigurationGroupId && g.Name == groupName))
                    .Select(e => new ConfigurationEntry
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
                        entry.Value = Helpers.DecryptString(Helpers.MachineKey, entry.Value);
                    }
                    catch
                    {
                        entry.Value = "";
                    }
                }

                return entry?.Value;
            });
        }

        public T? GetConfigurationValue<T>(string groupName, string entryName)
        {
            var value = GetConfigurationValue(groupName, entryName);
            return Converters.ConvertTo<T>(value.EnsureNotNull());
        }

        public T GetConfigurationValue<T>(string groupName, string entryName, T defaultValue)
        {
            var value = GetConfigurationValue(groupName, entryName);

            if (value == null)
            {
                return defaultValue;
            }

            return Converters.ConvertTo<T>(value) ?? defaultValue;
        }

        public void SaveConfigurationValue(string groupName, string entryName, string value)
        {
            var groupId = _db.ConfigurationGroups
                .Where(g => g.Name == groupName)
                .Select(g => g.Id)
                .Single();

            var entry = _db.ConfigurationEntries.Single(e => e.ConfigurationGroupId == groupId && e.Name == entryName);
            entry.Value = entry.IsEncrypted
                ? Helpers.EncryptString(Helpers.MachineKey, value)
                : value;

            _db.SaveChanges();

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            ReloadAllConfiguration();
        }

        public List<ConfigurationFlat> GetFlatConfiguration()
        {
            return (from g in _db.ConfigurationGroups.AsNoTracking()
                    join e in _db.ConfigurationEntries.AsNoTracking() on g.Id equals e.ConfigurationGroupId
                    select new ConfigurationFlat
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

        public List<ConfigurationNest> GetConfigurationNest()
        {
            var flatConfig = GetFlatConfiguration();
            var result = new List<ConfigurationNest>();

            var groups = flatConfig.GroupBy(o => o.GroupId).ToList();
            foreach (var group in groups)
            {
                var nest = new ConfigurationNest
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
                            entryValue = Helpers.DecryptString(Helpers.MachineKey, value.EntryValue);
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

                    nest.Entries.Add(new ConfigurationEntry
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

        #endregion

        #region Themes

        public List<Theme> GetAllThemes()
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Configuration);

            return WikiCache.AddOrGet(cacheKey, () =>
            {
                var themes = _db.Themes.AsNoTracking()
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

                foreach (var Theme in themes)
                {
                    Theme.Files = Theme.DelimitedFiles.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                }

                return themes;
            }).EnsureNotNull();
        }

        public Theme? GetThemeByName(string themeName)
        {
            return GetAllThemes().SingleOrDefault(t => t.Name == themeName);
        }

        #endregion

        #region Menu Items

        public List<MenuItem> GetAllMenuItems(string? orderBy = null, string? orderByDirection = null)
        {
            var asc = string.Equals(orderByDirection, "asc", StringComparison.OrdinalIgnoreCase);

            IQueryable<MenuItemDB> query = _db.MenuItems.AsNoTracking();

            query = (orderBy ?? string.Empty).ToLowerInvariant() switch
            {
                "name" => asc ? query.OrderBy(m => m.Name) : query.OrderByDescending(m => m.Name),
                "link" => asc ? query.OrderBy(m => m.Link) : query.OrderByDescending(m => m.Link),
                "ordinal" => asc ? query.OrderBy(m => m.Ordinal) : query.OrderByDescending(m => m.Ordinal),
                _ => query.OrderBy(m => m.Ordinal)
            };

            return query.Select(m => new MenuItem
            {
                Id = m.Id,
                Name = m.Name,
                Link = m.Link,
                Ordinal = m.Ordinal
            }).ToList();
        }

        public MenuItem GetMenuItemById(int id)
        {
            var item = _db.MenuItems.AsNoTracking().Single(m => m.Id == id);
            return new MenuItem { Id = item.Id, Name = item.Name, Link = item.Link, Ordinal = item.Ordinal };
        }

        public void DeleteMenuItem(int id)
        {
            var entity = _db.MenuItems.SingleOrDefault(m => m.Id == id);
            if (entity == null)
            {
                return;
            }

            _db.MenuItems.Remove(entity);
            _db.SaveChanges();

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            GlobalConfiguration.MenuItems = GetAllMenuItems();
        }

        public int UpdateMenuItem(MenuItem MenuItem)
        {
            var entity = _db.MenuItems.Single(m => m.Id == MenuItem.Id);
            entity.Name = MenuItem.Name;
            entity.Link = MenuItem.Link;
            entity.Ordinal = MenuItem.Ordinal;

            _db.SaveChanges();

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            GlobalConfiguration.MenuItems = GetAllMenuItems();

            return entity.Id;
        }

        public int CreateMenuItem(MenuItem MenuItem)
        {
            var entity = new MenuItemDB
            {
                Name = MenuItem.Name,
                Link = MenuItem.Link,
                Ordinal = MenuItem.Ordinal
            };

            _db.MenuItems.Add(entity);
            _db.SaveChanges();

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            GlobalConfiguration.MenuItems = GetAllMenuItems();

            return entity.Id;
        }

        #endregion

        #region Crypto/Security

        public bool ValidateCryptoSetup()
        {
            var value = _db.ConfigurationEntries.AsNoTracking()
                .Where(e => e.Name == "CryptoCheck")
                .Select(e => e.Value)
                .FirstOrDefault() ?? string.Empty;

            try
            {
                value = Helpers.DecryptString(Helpers.MachineKey, value);
                return value == Constants.CRYPTOCHECK;
            }
            catch
            {
                return false;
            }
        }

        public void InitializeCryptoSetup()
        {
            var encrypted = Helpers.EncryptString(Helpers.MachineKey, Constants.CRYPTOCHECK);

            var entry = _db.ConfigurationEntries.SingleOrDefault(e => e.Name == "CryptoCheck");
            if (entry == null)
            {
                entry = new DAL.Models.ConfigurationEntryDB
                {
                    Name = "CryptoCheck",
                    Value = encrypted,
                    IsEncrypted = true,
                    ConfigurationGroupId = 1, // Basic configuration group
                    DataType = "string",
                    Description = "Internal encryption validation key"
                };
                _db.ConfigurationEntries.Add(entry);
            }
            else
            {
                entry.Value = encrypted;
                entry.IsEncrypted = true;
            }

            _db.SaveChanges();
        }

        public bool IsFirstRun()
        {
            bool isEncryptionValid = ValidateCryptoSetup();
            if (isEncryptionValid == false)
            {
                InitializeCryptoSetup();
                return true;
            }
            return false;
        }

        #endregion

        #region Database Statistics

        public WikiDatabaseStatistics GetDatabaseStatistics()
        {
            return new WikiDatabaseStatistics
            {
                Pages = _db.Pages.Count(),
                IntraLinks = 0, // IntraLinks table not yet migrated - set to 0
                PageRevisions = _db.PageRevisions.Count(),
                PageAttachments = _db.PageFiles.Count(),
                PageAttachmentRevisions = _db.PageFileRevisions.Count(),
                PageTags = _db.PageTags.Count(),
                PageSearchTokens = _db.PageTokens.Count(),
                Users = _db.Profiles.Count(),
                Profiles = _db.Profiles.Count(),
                Exceptions = _db.WikiExceptions.Count(),
                Namespaces = _db.Pages.Select(p => p.Namespace).Distinct().Count()
            };
        }

        #endregion

        #region Configuration Reload

        public void ReloadAllConfiguration()
        {
            WikiCache.Clear();

            GlobalConfiguration.IsDebug = Debugger.IsAttached;

            var performanceConfig = GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.Performance);
            GlobalConfiguration.PageCacheSeconds = performanceConfig.Value<int>("Page Cache Time (Seconds)");
            GlobalConfiguration.RecordCompilationMetrics = performanceConfig.Value<bool>("Record Compilation Metrics");
            GlobalConfiguration.CacheMemoryLimitMB = performanceConfig.Value<int>("Cache Memory Limit MB");

            WikiCache.Initialize(GlobalConfiguration.CacheMemoryLimitMB, GlobalConfiguration.PageCacheSeconds);

            var basicConfig = GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.Basic);
            var customizationConfig = GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.Customization);
            var htmlConfig = GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.HTMLLayout);
            var functionalityConfig = GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.Functionality);
            var membershipConfig = GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.Membership);
            var searchConfig = GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.Search);
            var filesAndAttachmentsConfig = GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.FilesAndAttachments);
            var ldapAuthentication = GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.LDAPAuthentication);

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
        }

        #endregion
    }
}

