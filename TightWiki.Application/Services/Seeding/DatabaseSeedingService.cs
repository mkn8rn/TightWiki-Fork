using DAL;
using DAL.Models;
using Microsoft.Extensions.Logging;

namespace BLL.Services.Seeding
{
    /// <summary>
    /// Business logic service for database seeding operations.
    /// Seeds the database with initial configuration, themes, and other required data.
    /// Uses Entity Framework Core directly.
    /// </summary>
    public sealed class DatabaseSeedingService : IDatabaseSeedingService
    {
        private readonly WikiDbContext _db;
        private readonly ILogger<DatabaseSeedingService>? _logger;

        public DatabaseSeedingService(WikiDbContext db, ILogger<DatabaseSeedingService>? logger = null)
        {
            _db = db;
            _logger = logger;
        }

        public bool IsDatabaseSeeded()
        {
            return _db.Roles.Any() && _db.ConfigurationGroups.Any() && _db.Themes.Any();
        }

        public void SeedDatabase()
        {
            _logger?.LogInformation("DatabaseSeedingService: Starting database seeding...");

            try
            {
                // Check if database is accessible
                _logger?.LogInformation("DatabaseSeedingService: Testing database connection...");
                var canConnect = _db.Database.CanConnect();
                _logger?.LogInformation($"DatabaseSeedingService: Database connection test: {(canConnect ? "SUCCESS" : "FAILED")}");

                if (!canConnect)
                {
                    _logger?.LogError("DatabaseSeedingService: Cannot connect to database. Aborting seeding.");
                    return;
                }

                // Log table existence
                LogExistingData("before seeding");

                using var transaction = _db.Database.BeginTransaction();
                try
                {
                    SeedPermissionDispositions();
                    SeedRoles();
                    SeedPermissions();
                    SeedRolePermissions();
                    SeedConfigurationGroups();
                    SeedConfigurationEntries();
                    SeedThemes();
                    SeedMenuItems();
                    SeedBuiltInPages();

                    _logger?.LogInformation("DatabaseSeedingService: Committing transaction...");
                    transaction.Commit();
                    _logger?.LogInformation("DatabaseSeedingService: Transaction committed successfully.");
                }
                catch (System.Exception ex)
                {
                    _logger?.LogError(ex, "DatabaseSeedingService: Error during seeding, rolling back transaction.");
                    transaction.Rollback();
                    throw;
                }

                // Verify seeding
                LogExistingData("after seeding");

                _logger?.LogInformation("DatabaseSeedingService: Seeding completed successfully.");
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, "DatabaseSeedingService: Fatal error during seeding.");
                Console.WriteLine($"DatabaseSeedingService FATAL ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        private void LogExistingData(string phase)
        {
            _logger?.LogInformation($"DatabaseSeedingService: Checking existing data {phase}...");
            _logger?.LogInformation($"  - PermissionDispositions count: {_db.PermissionDispositions.Count()}");
            _logger?.LogInformation($"  - Roles count: {_db.Roles.Count()}");
            _logger?.LogInformation($"  - Permissions count: {_db.Permissions.Count()}");
            _logger?.LogInformation($"  - RolePermissions count: {_db.RolePermissions.Count()}");
            _logger?.LogInformation($"  - ConfigurationGroups count: {_db.ConfigurationGroups.Count()}");
            _logger?.LogInformation($"  - ConfigurationEntries count: {_db.ConfigurationEntries.Count()}");
            _logger?.LogInformation($"  - Themes count: {_db.Themes.Count()}");
            _logger?.LogInformation($"  - MenuItems count: {_db.MenuItems.Count()}");
            _logger?.LogInformation($"  - Pages count: {_db.Pages.Count()}");
        }

        private void SeedPermissionDispositions()
        {
            if (_db.PermissionDispositions.Any())
            {
                _logger?.LogInformation("DatabaseSeedingService: PermissionDispositions already seeded, skipping.");
                return;
            }

            _logger?.LogInformation("DatabaseSeedingService: Seeding PermissionDispositions...");
            var dispositions = new[]
            {
                new PermissionDispositionDB { Id = 1, Name = "Allow" },
                new PermissionDispositionDB { Id = 2, Name = "Deny" },
            };

            _db.PermissionDispositions.AddRange(dispositions);
            _db.SaveChanges();
            _logger?.LogInformation($"DatabaseSeedingService: Seeded {dispositions.Length} PermissionDispositions.");
        }

        private void SeedRoles()
        {
            if (_db.Roles.Any())
            {
                _logger?.LogInformation("DatabaseSeedingService: Roles already seeded, skipping.");
                return;
            }

            _logger?.LogInformation("DatabaseSeedingService: Seeding Roles...");
            var roles = new[]
            {
                new RoleDB { Id = 1, Name = "Administrator", Description = "Administrators can do anything. Add, edit, delete, etc.", IsBuiltIn = true },
                new RoleDB { Id = 2, Name = "Member", Description = "Read-only user with a profile.", IsBuiltIn = true },
                new RoleDB { Id = 3, Name = "Contributor", Description = "Contributor can add and edit unprotected pages.", IsBuiltIn = true },
                new RoleDB { Id = 4, Name = "Moderator", Description = "Moderators can add, edit, and delete pages - including protected pages.", IsBuiltIn = true },
                new RoleDB { Id = 5, Name = "Anonymous", Description = "Role applied to users who are not logged in.", IsBuiltIn = true },
            };

            _db.Roles.AddRange(roles);
            _db.SaveChanges();
            _logger?.LogInformation($"DatabaseSeedingService: Seeded {roles.Length} Roles.");
        }

        private void SeedPermissions()
        {
            if (_db.Permissions.Any())
            {
                _logger?.LogInformation("DatabaseSeedingService: Permissions already seeded, skipping.");
                return;
            }

            _logger?.LogInformation("DatabaseSeedingService: Seeding Permissions...");
            var permissions = new[]
            {
                new PermissionDB { Id = 1, Name = "Read", Description = "User or role can read page or within namespace." },
                new PermissionDB { Id = 2, Name = "Edit", Description = "User or role can edit page or within namespace." },
                new PermissionDB { Id = 3, Name = "Delete", Description = "User or role can delete page or within namespace." },
                new PermissionDB { Id = 4, Name = "Moderate", Description = "User or role can moderate page or within namespace, such as editing protected pages and reverting changes." },
                new PermissionDB { Id = 5, Name = "Create", Description = "User or role can create pages." },
            };

            _db.Permissions.AddRange(permissions);
            _db.SaveChanges();
            _logger?.LogInformation($"DatabaseSeedingService: Seeded {permissions.Length} Permissions.");
        }

        private void SeedRolePermissions()
        {
            if (_db.RolePermissions.Any())
            {
                _logger?.LogInformation("DatabaseSeedingService: RolePermissions already seeded, skipping.");
                return;
            }

            _logger?.LogInformation("DatabaseSeedingService: Seeding RolePermissions...");
            var rolePermissions = new List<RolePermissionEntityDB>();

            // Administrator - all permissions on all pages and namespaces
            foreach (var permId in new[] { 1, 2, 3, 4, 5 })
            {
                rolePermissions.Add(new RolePermissionEntityDB { RoleId = 1, PermissionId = permId, PageId = "*", Namespace = null, PermissionDispositionId = 1 });
                rolePermissions.Add(new RolePermissionEntityDB { RoleId = 1, PermissionId = permId, PageId = null, Namespace = "*", PermissionDispositionId = 1 });
            }

            // Anonymous - read only
            rolePermissions.Add(new RolePermissionEntityDB { RoleId = 5, PermissionId = 1, PageId = "*", Namespace = null, PermissionDispositionId = 1 });
            rolePermissions.Add(new RolePermissionEntityDB { RoleId = 5, PermissionId = 1, PageId = null, Namespace = "*", PermissionDispositionId = 1 });

            // Member - read only
            rolePermissions.Add(new RolePermissionEntityDB { RoleId = 2, PermissionId = 1, PageId = "*", Namespace = null, PermissionDispositionId = 1 });
            rolePermissions.Add(new RolePermissionEntityDB { RoleId = 2, PermissionId = 1, PageId = null, Namespace = "*", PermissionDispositionId = 1 });

            // Contributor - read, edit, create
            foreach (var permId in new[] { 1, 2, 5 })
            {
                rolePermissions.Add(new RolePermissionEntityDB { RoleId = 3, PermissionId = permId, PageId = "*", Namespace = null, PermissionDispositionId = 1 });
                rolePermissions.Add(new RolePermissionEntityDB { RoleId = 3, PermissionId = permId, PageId = null, Namespace = "*", PermissionDispositionId = 1 });
            }

            // Moderator - read, edit, delete, moderate, create
            foreach (var permId in new[] { 1, 2, 3, 4, 5 })
            {
                rolePermissions.Add(new RolePermissionEntityDB { RoleId = 4, PermissionId = permId, PageId = "*", Namespace = null, PermissionDispositionId = 1 });
                rolePermissions.Add(new RolePermissionEntityDB { RoleId = 4, PermissionId = permId, PageId = null, Namespace = "*", PermissionDispositionId = 1 });
            }

            _db.RolePermissions.AddRange(rolePermissions);
            _db.SaveChanges();
            _logger?.LogInformation($"DatabaseSeedingService: Seeded {rolePermissions.Count} RolePermissions.");
        }

        private void SeedConfigurationGroups()
        {
            if (_db.ConfigurationGroups.Any())
            {
                _logger?.LogInformation("DatabaseSeedingService: ConfigurationGroups already seeded, skipping.");
                return;
            }

            _logger?.LogInformation("DatabaseSeedingService: Seeding ConfigurationGroups...");
            var groups = new[]
            {
                new ConfigurationGroupDB { Id = 1, Name = "Basic", Description = "Basic site configuration." },
                new ConfigurationGroupDB { Id = 2, Name = "HTML Layout", Description = "Customizable HTML layout configuration." },
                new ConfigurationGroupDB { Id = 3, Name = "Functionality", Description = "Feature toggle configuration." },
                new ConfigurationGroupDB { Id = 4, Name = "Search", Description = "Search and indexing configuration." },
                new ConfigurationGroupDB { Id = 5, Name = "Performance", Description = "Performance and caching configuration." },
                new ConfigurationGroupDB { Id = 6, Name = "Membership", Description = "User configuration" },
                new ConfigurationGroupDB { Id = 7, Name = "External Authentication", Description = "External authentication providers." },
                new ConfigurationGroupDB { Id = 8, Name = "Cookies", Description = "Cookie configuration." },
                new ConfigurationGroupDB { Id = 9, Name = "Email", Description = "Email configuration." },
                new ConfigurationGroupDB { Id = 10, Name = "LDAP Authentication", Description = "LDAP authentication configuration." },
                new ConfigurationGroupDB { Id = 11, Name = "Customization", Description = "Site customization settings." },
                new ConfigurationGroupDB { Id = 12, Name = "Files and Attachments", Description = "File upload and attachment settings." },
            };

            _db.ConfigurationGroups.AddRange(groups);
            _db.SaveChanges();
            _logger?.LogInformation($"DatabaseSeedingService: Seeded {groups.Length} ConfigurationGroups.");
        }

        private void SeedConfigurationEntries()
        {
            if (_db.ConfigurationEntries.Any())
            {
                _logger?.LogInformation("DatabaseSeedingService: ConfigurationEntries already seeded, skipping.");
                return;
            }

            _logger?.LogInformation("DatabaseSeedingService: Seeding ConfigurationEntries...");
            var entries = new List<ConfigurationEntryDB>
            {
                // Basic (GroupId = 1)
                new() { ConfigurationGroupId = 1, Name = "Name", Value = "TightWiki", DataType = "string", Description = "The name of the wiki.", IsEncrypted = false },
                new() { ConfigurationGroupId = 1, Name = "Address", Value = "", DataType = "string", Description = "The address/URL of the wiki.", IsEncrypted = false },
                new() { ConfigurationGroupId = 1, Name = "Copyright", Value = "Powered by TightWiki", DataType = "string", Description = "The copyright/footer.", IsEncrypted = false },

                // HTML Layout (GroupId = 2)
                new() { ConfigurationGroupId = 2, Name = "Pre-Body", Value = "", DataType = "string", Description = "HTML which is injected into the <body> tag.", IsEncrypted = false },
                new() { ConfigurationGroupId = 2, Name = "Post-Body", Value = "", DataType = "string", Description = "HTML which is injected after the </body> tag.", IsEncrypted = false },
                new() { ConfigurationGroupId = 2, Name = "Header", Value = "", DataType = "string", Description = "HTML which is injected into the <head> tag.", IsEncrypted = false },
                new() { ConfigurationGroupId = 2, Name = "Footer", Value = "", DataType = "string", Description = "HTML which is injected into the page footer.", IsEncrypted = false },

                // Functionality (GroupId = 3)
                new() { ConfigurationGroupId = 3, Name = "Include wiki Description in Meta", Value = "true", DataType = "bool", Description = "Include wiki description in page meta tags.", IsEncrypted = false },
                new() { ConfigurationGroupId = 3, Name = "Include wiki Tags in Meta", Value = "true", DataType = "bool", Description = "Include wiki tags in page meta tags.", IsEncrypted = false },
                new() { ConfigurationGroupId = 3, Name = "Enable Page Comments", Value = "true", DataType = "bool", Description = "If true, page comments will be enabled.", IsEncrypted = false },
                new() { ConfigurationGroupId = 3, Name = "Enable Public Profiles", Value = "true", DataType = "bool", Description = "If true, user profiles are publicly visible.", IsEncrypted = false },
                new() { ConfigurationGroupId = 3, Name = "Show Comments on Page Footer", Value = "true", DataType = "bool", Description = "If true, comments shown on page footer.", IsEncrypted = false },
                new() { ConfigurationGroupId = 3, Name = "Show Change Summary when Editing", Value = "true", DataType = "bool", Description = "If true, change summary is shown when editing.", IsEncrypted = false },
                new() { ConfigurationGroupId = 3, Name = "Require Change Summary when Editing", Value = "false", DataType = "bool", Description = "If true, change summary is required when editing.", IsEncrypted = false },
                new() { ConfigurationGroupId = 3, Name = "Show Last Modified on Page Footer", Value = "true", DataType = "bool", Description = "If true, last modified info shown on page footer.", IsEncrypted = false },

                // Search (GroupId = 4)
                new() { ConfigurationGroupId = 4, Name = "Include Search on Navbar", Value = "true", DataType = "bool", Description = "If true, search box is shown on navbar.", IsEncrypted = false },
                new() { ConfigurationGroupId = 4, Name = "Word Exclusions", Value = "do,of,it,i,is,or,and,but,of,the,a,for,also,be,it,as,that,this,it,to,on,are,if,in", DataType = "string", Description = "Words excluded from search indexing.", IsEncrypted = false },
                new() { ConfigurationGroupId = 4, Name = "Split Camel Case", Value = "true", DataType = "bool", Description = "Split CamelCase when indexing.", IsEncrypted = false },
                new() { ConfigurationGroupId = 4, Name = "Minimum Match Score", Value = "0.60", DataType = "decimal", Description = "Minimum match score for search results.", IsEncrypted = false },
                new() { ConfigurationGroupId = 4, Name = "Allow Fuzzy Matching", Value = "true", DataType = "bool", Description = "Allow fuzzy matching in search.", IsEncrypted = false },

                // Performance (GroupId = 5)
                new() { ConfigurationGroupId = 5, Name = "Page Cache Time (Seconds)", Value = "300", DataType = "int", Description = "Default page cache duration in seconds.", IsEncrypted = false },
                new() { ConfigurationGroupId = 5, Name = "Record Compilation Metrics", Value = "false", DataType = "bool", Description = "If true, compilation metrics are recorded.", IsEncrypted = false },
                new() { ConfigurationGroupId = 5, Name = "Cache Memory Limit MB", Value = "500", DataType = "int", Description = "Maximum memory for cache in MB.", IsEncrypted = false },
                new() { ConfigurationGroupId = 5, Name = "Default Profile Recently Modified Count", Value = "10", DataType = "int", Description = "Number of recently modified items on profile.", IsEncrypted = false },
                new() { ConfigurationGroupId = 5, Name = "Pre-Load Animated Emojis", Value = "false", DataType = "bool", Description = "If true, animated emojis are preloaded.", IsEncrypted = false },

                // Membership (GroupId = 6)
                new() { ConfigurationGroupId = 6, Name = "Allow Signup", Value = "true", DataType = "bool", Description = "If true, users can create accounts.", IsEncrypted = false },
                new() { ConfigurationGroupId = 6, Name = "Default TimeZone", Value = "UTC", DataType = "string", Description = "Default timezone for new users.", IsEncrypted = false },
                new() { ConfigurationGroupId = 6, Name = "Default Country", Value = "US", DataType = "string", Description = "Default country for new users.", IsEncrypted = false },
                new() { ConfigurationGroupId = 6, Name = "Default Language", Value = "en", DataType = "string", Description = "Default language for new users.", IsEncrypted = false },
                new() { ConfigurationGroupId = 6, Name = "Default Signup Role", Value = "Contributor", DataType = "string", Description = "Default role for new signups.", IsEncrypted = false },
                new() { ConfigurationGroupId = 6, Name = "Count Of Failed Login Attempts Before Lockout", Value = "5", DataType = "int", Description = "Number of failed attempts before lockout.", IsEncrypted = false },
                new() { ConfigurationGroupId = 6, Name = "Failed Login Lockout Duration (minutes)", Value = "15", DataType = "int", Description = "Lockout duration in minutes.", IsEncrypted = false },
                new() { ConfigurationGroupId = 6, Name = "Require Email Verification", Value = "false", DataType = "bool", Description = "If true, email verification is required.", IsEncrypted = false },
                new() { ConfigurationGroupId = 6, Name = "Request Password Complexity", Value = "true", DataType = "bool", Description = "If true, passwords must be complex.", IsEncrypted = false },
                new() { ConfigurationGroupId = 6, Name = "Account Verification Email Subject", Value = "Account Verification", DataType = "string", Description = "Subject for verification email.", IsEncrypted = false },
                new() { ConfigurationGroupId = 6, Name = "Account Verification Email Template", Value = "<p>Please click <a href=\"##Link##\">here</a> to verify your account.</p>", DataType = "string", Description = "Body template for verification email.", IsEncrypted = false },
                new() { ConfigurationGroupId = 6, Name = "Password Reset Email Subject", Value = "Password Reset", DataType = "string", Description = "Subject for password reset email.", IsEncrypted = false },
                new() { ConfigurationGroupId = 6, Name = "Password Reset Email Template", Value = "<p>Please click <a href=\"##Link##\">here</a> to reset your password.</p>", DataType = "string", Description = "Body template for password reset email.", IsEncrypted = false },
                new() { ConfigurationGroupId = 6, Name = "Template: Account Verification Email", Value = "<p>Please click <a href=\"##CALLBACKURL##\">here</a> to verify your account.</p>", DataType = "string", Description = "HTML email template for new account verification.", IsEncrypted = false },
                new() { ConfigurationGroupId = 6, Name = "Template: Reset Password Email", Value = "<p>Please click <a href=\"##CALLBACKURL##\">here</a> to reset your password.</p>", DataType = "string", Description = "HTML email template for password reset.", IsEncrypted = false },

                // External Authentication (GroupId = 7)
                new() { ConfigurationGroupId = 7, Name = "Google : Use Google Authentication", Value = "false", DataType = "bool", Description = "Enable Google authentication.", IsEncrypted = false },
                new() { ConfigurationGroupId = 7, Name = "Google : ClientId", Value = "", DataType = "string", Description = "Google OAuth Client ID.", IsEncrypted = false },
                new() { ConfigurationGroupId = 7, Name = "Google : ClientSecret", Value = "", DataType = "string", Description = "Google OAuth Client Secret.", IsEncrypted = true },
                new() { ConfigurationGroupId = 7, Name = "Microsoft : Use Microsoft Authentication", Value = "false", DataType = "bool", Description = "Enable Microsoft authentication.", IsEncrypted = false },
                new() { ConfigurationGroupId = 7, Name = "Microsoft : ClientId", Value = "", DataType = "string", Description = "Microsoft OAuth Client ID.", IsEncrypted = false },
                new() { ConfigurationGroupId = 7, Name = "Microsoft : ClientSecret", Value = "", DataType = "string", Description = "Microsoft OAuth Client Secret.", IsEncrypted = true },
                new() { ConfigurationGroupId = 7, Name = "OIDC : Use OIDC Authentication", Value = "false", DataType = "bool", Description = "Enable OIDC authentication.", IsEncrypted = false },
                new() { ConfigurationGroupId = 7, Name = "OIDC : Authority", Value = "", DataType = "string", Description = "OIDC Authority URL.", IsEncrypted = false },
                new() { ConfigurationGroupId = 7, Name = "OIDC : ClientId", Value = "", DataType = "string", Description = "OIDC Client ID.", IsEncrypted = false },
                new() { ConfigurationGroupId = 7, Name = "OIDC : ClientSecret", Value = "", DataType = "string", Description = "OIDC Client Secret.", IsEncrypted = true },

                // Cookies (GroupId = 8)
                new() { ConfigurationGroupId = 8, Name = "Expiration Hours", Value = "720", DataType = "int", Description = "Cookie expiration in hours.", IsEncrypted = false },
                new() { ConfigurationGroupId = 8, Name = "Persist Keys Path", Value = "", DataType = "string", Description = "Path to persist data protection keys.", IsEncrypted = false },

                // Email (GroupId = 9)
                new() { ConfigurationGroupId = 9, Name = "Address", Value = "", DataType = "string", Description = "SMTP server address.", IsEncrypted = false },
                new() { ConfigurationGroupId = 9, Name = "Port", Value = "587", DataType = "int", Description = "SMTP server port.", IsEncrypted = false },
                new() { ConfigurationGroupId = 9, Name = "Username", Value = "", DataType = "string", Description = "SMTP username.", IsEncrypted = false },
                new() { ConfigurationGroupId = 9, Name = "Password", Value = "", DataType = "string", Description = "SMTP password.", IsEncrypted = true },
                new() { ConfigurationGroupId = 9, Name = "From Display Name", Value = "TightWiki", DataType = "string", Description = "Display name for outgoing emails.", IsEncrypted = false },
                new() { ConfigurationGroupId = 9, Name = "Use SSL", Value = "true", DataType = "bool", Description = "Use SSL for SMTP connection.", IsEncrypted = false },

                // LDAP Authentication (GroupId = 10)
                new() { ConfigurationGroupId = 10, Name = "LDAP : Enable LDAP Authentication", Value = "false", DataType = "bool", Description = "Enable LDAP authentication.", IsEncrypted = false },
                new() { ConfigurationGroupId = 10, Name = "LDAP : Fully-Qualified Domain", Value = "", DataType = "string", Description = "The fully qualified domain name for LDAP.", IsEncrypted = false },
                new() { ConfigurationGroupId = 10, Name = "LDAP : Use Secure Socket Layer", Value = "false", DataType = "bool", Description = "Use SSL for LDAP connection.", IsEncrypted = false },
                new() { ConfigurationGroupId = 10, Name = "LDAP : Default Sign-in Domain", Value = "", DataType = "string", Description = "Default domain when user doesn't specify one.", IsEncrypted = false },

                // Customization (GroupId = 11)
                new() { ConfigurationGroupId = 11, Name = "Theme", Value = "Darkly", DataType = "string", Description = "The default color theme of the site.", IsEncrypted = false },
                new() { ConfigurationGroupId = 11, Name = "Fixed Header Menu Position", Value = "true", DataType = "bool", Description = "If true, header menu stays fixed.", IsEncrypted = false },
                new() { ConfigurationGroupId = 11, Name = "Default Emoji Height", Value = "24", DataType = "int", Description = "Default emoji height in pixels.", IsEncrypted = false },
                new() { ConfigurationGroupId = 11, Name = "Pagination Size", Value = "20", DataType = "int", Description = "Number of items per page.", IsEncrypted = false },
                new() { ConfigurationGroupId = 11, Name = "Default TimeZone", Value = "UTC", DataType = "string", Description = "Default timezone for display.", IsEncrypted = false },
                new() { ConfigurationGroupId = 11, Name = "Default Country", Value = "US", DataType = "string", Description = "Default country for display.", IsEncrypted = false },
                new() { ConfigurationGroupId = 11, Name = "Default Language", Value = "en", DataType = "string", Description = "Default language for display.", IsEncrypted = false },
                new() { ConfigurationGroupId = 11, Name = "Brand Image (Small)", Value = "/images/TightWiki Icon 32.png", DataType = "string", Description = "Small brand image path.", IsEncrypted = false },
                new() { ConfigurationGroupId = 11, Name = "FooterBlurb", Value = "", DataType = "string", Description = "Footer blurb text.", IsEncrypted = false },
                new() { ConfigurationGroupId = 11, Name = "Page Not Exists Page", Value = "Builtin :: Wiki Page Does Not Exist", DataType = "string", Description = "The name of the wiki page to display when a non-existing page is requested.", IsEncrypted = false },
                new() { ConfigurationGroupId = 11, Name = "Revision Does Not Exists Page", Value = "Builtin :: Wiki Page Revision Does Not Exist", DataType = "string", Description = "The name of the wiki page to display when a non-existing page revision is requested.", IsEncrypted = false },
                new() { ConfigurationGroupId = 11, Name = "New Page Template", Value = "Builtin :: Wiki Default Page", DataType = "string", Description = "The name of the wiki page to use as the default content when new wiki pages are created.", IsEncrypted = false },

                // Files and Attachments (GroupId = 12)
                new() { ConfigurationGroupId = 12, Name = "Max Avatar File Size", Value = "1048576", DataType = "int", Description = "Maximum avatar file size in bytes.", IsEncrypted = false },
                new() { ConfigurationGroupId = 12, Name = "Max Attachment File Size", Value = "10485760", DataType = "int", Description = "Maximum attachment file size in bytes.", IsEncrypted = false },
                new() { ConfigurationGroupId = 12, Name = "Max Emoji File Size", Value = "524288", DataType = "int", Description = "Maximum emoji file size in bytes.", IsEncrypted = false },
            };

            _db.ConfigurationEntries.AddRange(entries);
            _db.SaveChanges();
            _logger?.LogInformation($"DatabaseSeedingService: Seeded {entries.Count} ConfigurationEntries.");
        }

        private void SeedThemes()
        {
            if (_db.Themes.Any())
            {
                _logger?.LogInformation("DatabaseSeedingService: Themes already seeded, skipping.");
                return;
            }

            _logger?.LogInformation("DatabaseSeedingService: Seeding Themes...");
            var themes = new[]
            {
                new ThemeDB { Name = "Cerulean", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/cerulean/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new ThemeDB { Name = "Cosmo", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/cosmo/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new ThemeDB { Name = "Cyborg", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/cyborg/bootstrap.min.css;/css/dark.css;/syntax/styles/dark.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-white", ClassDropdown = "text-white", ClassBranding = "text-white", EditorTheme = "dark" },
                new ThemeDB { Name = "Darkly", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/darkly/bootstrap.min.css;/css/dark.css;/syntax/styles/dark.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-light", ClassDropdown = "text-light", ClassBranding = "text-light", EditorTheme = "dark" },
                new ThemeDB { Name = "Flatly", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/flatly/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new ThemeDB { Name = "Journal", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/journal/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new ThemeDB { Name = "Light", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootstrap/5.3.7/css/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-light bg-light", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new ThemeDB { Name = "Litera", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/litera/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new ThemeDB { Name = "Lumen", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/lumen/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-light", ClassDropdown = "text-dark", ClassBranding = "text-light", EditorTheme = "light" },
                new ThemeDB { Name = "Lux", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/lux/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-light", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new ThemeDB { Name = "Materia", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/materia/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new ThemeDB { Name = "Minty", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/minty/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new ThemeDB { Name = "Morph", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/morph/bootstrap.min.css;/css/gray.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new ThemeDB { Name = "Pulse", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/pulse/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-light", ClassDropdown = "text-dark", ClassBranding = "text-light", EditorTheme = "light" },
                new ThemeDB { Name = "Quartz", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/quartz/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new ThemeDB { Name = "Sandstone", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/sandstone/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-secondary", ClassDropdown = "text-primary", ClassBranding = "text-light", EditorTheme = "light" },
                new ThemeDB { Name = "Simplex", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/simplex/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-light", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new ThemeDB { Name = "Sketchy", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/sketchy/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-light", ClassDropdown = "text-dark", ClassBranding = "text-light", EditorTheme = "light" },
                new ThemeDB { Name = "Slate", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/slate/bootstrap.min.css;/css/gray.css;/syntax/styles/dark.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-light", ClassDropdown = "text-light", ClassBranding = "text-light", EditorTheme = "dark" },
                new ThemeDB { Name = "Solar", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/solar/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new ThemeDB { Name = "Spacelab", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/spacelab/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new ThemeDB { Name = "Superhero", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/superhero/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new ThemeDB { Name = "United", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/united/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new ThemeDB { Name = "Vapor", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/vapor/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new ThemeDB { Name = "Yeti", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/yeti/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new ThemeDB { Name = "Zephyr", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/zephyr/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
            };

            _db.Themes.AddRange(themes);
            _db.SaveChanges();
            _logger?.LogInformation($"DatabaseSeedingService: Seeded {themes.Length} Themes.");
        }

        private void SeedMenuItems()
        {
            if (_db.MenuItems.Any())
            {
                _logger?.LogInformation("DatabaseSeedingService: MenuItems already seeded, skipping.");
                return;
            }

            _logger?.LogInformation("DatabaseSeedingService: Seeding MenuItems...");
            var menuItems = new[]
            {
                new MenuItemDB { Id = 1, Name = "Home", Link = "/Home", Ordinal = 1 },
            };

            _db.MenuItems.AddRange(menuItems);
            _db.SaveChanges();
            _logger?.LogInformation($"DatabaseSeedingService: Seeded {menuItems.Length} MenuItems.");
        }

        private void SeedBuiltInPages()
        {
            if (_db.Pages.Any())
            {
                _logger?.LogInformation("DatabaseSeedingService: Pages already seeded, skipping.");
                return;
            }

            _logger?.LogInformation("DatabaseSeedingService: Seeding built-in Pages...");

            var systemUserId = Guid.Empty; // System user for built-in pages
            var now = DateTime.UtcNow;

            var builtInPages = new[]
            {
                new
                {
                    Name = "Builtin :: Wiki Page Does Not Exist",
                    Navigation = "builtin::wiki_page_does_not_exist",
                    Namespace = "Builtin",
                    Description = "This is the content that is displayed when a wiki pages is requested that does not exist.",
                    Body = @"@@HideFooterComments @@HideFooterLastModified @@protect(true)
@@Tags(Config)
{{Alert(Warning, Page does not exist)

The page you requested does not exist. Why don't you create it?


If you need help, check out the [[Wiki Help :: Wiki Help, official wiki help page]]
}}

"
                },
                new
                {
                    Name = "Builtin :: Wiki Page Revision Does Not Exist",
                    Navigation = "builtin::wiki_page_revision_does_not_exist",
                    Namespace = "Builtin",
                    Description = "This is the content that is displayed when a wiki page revision is requested that does not exist.",
                    Body = @"@@HideFooterComments @@HideFooterLastModified @@protect(true)
@@Tags(Config)
{{Alert(Warning, Page revision does not exist)

The page revision you requested does not exist.

If you need help, check out the [[Wiki Help :: Wiki Help, official wiki help page]]
}}

"
                },
                new
                {
                    Name = "Builtin :: Wiki Default Page",
                    Navigation = "builtin::wiki_default_page",
                    Namespace = "Builtin",
                    Description = "This pages content will be used as the default content for new pages when they are created.",
                    Body = @"@@draft
##title @@Tags(Draft)
{{Card(Default, Table of Contents) ##toc }}

==Overview

===Point #1

===Point #2

==Related
##related

"
                },
                new
                {
                    Name = "Home",
                    Navigation = "home",
                    Namespace = "",
                    Description = "The home page",
                    Body = @"==Welcome to TightWiki==

This is your wiki home page. Edit it to customize your wiki.

"
                }
            };

            var pageId = 1;
            foreach (var builtIn in builtInPages)
            {
                var page = new PageEntityDB
                {
                    Id = pageId,
                    Name = builtIn.Name,
                    Navigation = builtIn.Navigation,
                    Namespace = builtIn.Namespace,
                    Description = builtIn.Description,
                    Revision = 1,
                    CreatedByUserId = systemUserId,
                    CreatedDate = now,
                    ModifiedByUserId = systemUserId,
                    ModifiedDate = now
                };

                var pageRevision = new PageRevisionEntityDB
                {
                    PageId = pageId,
                    Revision = 1,
                    Name = builtIn.Name,
                    Navigation = builtIn.Navigation,
                    Namespace = builtIn.Namespace,
                    Description = builtIn.Description,
                    Body = builtIn.Body,
                    DataHash = builtIn.Body.GetHashCode(),
                    ChangeSummary = "Initial version",
                    ModifiedByUserId = systemUserId,
                    ModifiedDate = now
                };

                _db.Pages.Add(page);
                _db.PageRevisions.Add(pageRevision);
                pageId++;
            }

            _db.SaveChanges();
            _logger?.LogInformation($"DatabaseSeedingService: Seeded {builtInPages.Length} built-in Pages.");
        }
    }
}
