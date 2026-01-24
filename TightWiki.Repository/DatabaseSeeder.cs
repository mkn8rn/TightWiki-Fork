using DAL;
using DAL.Models;
using Microsoft.Extensions.Logging;

namespace TightWiki.Repository
{
    /// <summary>
    /// Seeds the database with initial configuration, themes, and other required data.
    /// This replaces the SQLite-based DatabaseUpgrade seeding mechanism.
    /// </summary>
    public static class DatabaseSeeder
    {
        /// <summary>
        /// Seeds the database with required initial data if tables are empty.
        /// </summary>
        public static void SeedDatabase(WikiDbContext db, ILogger? logger = null)
        {
            logger?.LogInformation("DatabaseSeeder: Starting database seeding...");
            
            try
            {
                // Check if database is accessible
                logger?.LogInformation("DatabaseSeeder: Testing database connection...");
                var canConnect = db.Database.CanConnect();
                logger?.LogInformation($"DatabaseSeeder: Database connection test: {(canConnect ? "SUCCESS" : "FAILED")}");
                
                if (!canConnect)
                {
                    logger?.LogError("DatabaseSeeder: Cannot connect to database. Aborting seeding.");
                    return;
                }

                // Log table existence
                logger?.LogInformation("DatabaseSeeder: Checking existing data...");
                logger?.LogInformation($"  - PermissionDispositions count: {db.PermissionDispositions.Count()}");
                logger?.LogInformation($"  - Roles count: {db.Roles.Count()}");
                logger?.LogInformation($"  - Permissions count: {db.Permissions.Count()}");
                logger?.LogInformation($"  - RolePermissions count: {db.RolePermissions.Count()}");
                logger?.LogInformation($"  - ConfigurationGroups count: {db.ConfigurationGroups.Count()}");
                logger?.LogInformation($"  - ConfigurationEntries count: {db.ConfigurationEntries.Count()}");
                logger?.LogInformation($"  - Themes count: {db.Themes.Count()}");
                logger?.LogInformation($"  - MenuItems count: {db.MenuItems.Count()}");
                logger?.LogInformation($"  - Pages count: {db.Pages.Count()}");

                using var transaction = db.Database.BeginTransaction();
                try
                {
                    SeedPermissionDispositions(db, logger);
                    SeedRoles(db, logger);
                    SeedPermissions(db, logger);
                    SeedRolePermissions(db, logger);
                    SeedConfigurationGroups(db, logger);
                    SeedConfigurationEntries(db, logger);
                    SeedThemes(db, logger);
                    SeedMenuItems(db, logger);
                    SeedBuiltInPages(db, logger);
                    
                    logger?.LogInformation("DatabaseSeeder: Committing transaction...");
                    transaction.Commit();
                    logger?.LogInformation("DatabaseSeeder: Transaction committed successfully.");
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "DatabaseSeeder: Error during seeding, rolling back transaction.");
                    transaction.Rollback();
                    throw;
                }

                // Verify seeding
                logger?.LogInformation("DatabaseSeeder: Verifying seeded data...");
                logger?.LogInformation($"  - PermissionDispositions count: {db.PermissionDispositions.Count()}");
                logger?.LogInformation($"  - Roles count: {db.Roles.Count()}");
                logger?.LogInformation($"  - Permissions count: {db.Permissions.Count()}");
                logger?.LogInformation($"  - RolePermissions count: {db.RolePermissions.Count()}");
                logger?.LogInformation($"  - ConfigurationGroups count: {db.ConfigurationGroups.Count()}");
                logger?.LogInformation($"  - ConfigurationEntries count: {db.ConfigurationEntries.Count()}");
                logger?.LogInformation($"  - Themes count: {db.Themes.Count()}");
                logger?.LogInformation($"  - MenuItems count: {db.MenuItems.Count()}");
                logger?.LogInformation($"  - Pages count: {db.Pages.Count()}");
                
                logger?.LogInformation("DatabaseSeeder: Seeding completed successfully.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "DatabaseSeeder: Fatal error during seeding.");
                Console.WriteLine($"DatabaseSeeder FATAL ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        private static void SeedPermissionDispositions(WikiDbContext db, ILogger? logger)
        {
            if (db.PermissionDispositions.Any())
            {
                logger?.LogInformation("DatabaseSeeder: PermissionDispositions already seeded, skipping.");
                return;
            }

            logger?.LogInformation("DatabaseSeeder: Seeding PermissionDispositions...");
            var dispositions = new[]
            {
                new PermissionDisposition { Id = 1, Name = "Allow" },
                new PermissionDisposition { Id = 2, Name = "Deny" },
            };

            db.PermissionDispositions.AddRange(dispositions);
            db.SaveChanges();
            logger?.LogInformation($"DatabaseSeeder: Seeded {dispositions.Length} PermissionDispositions.");
        }

        private static void SeedRoles(WikiDbContext db, ILogger? logger)
        {
            if (db.Roles.Any())
            {
                logger?.LogInformation("DatabaseSeeder: Roles already seeded, skipping.");
                return;
            }

            logger?.LogInformation("DatabaseSeeder: Seeding Roles...");
            var roles = new[]
            {
                new Role { Id = 1, Name = "Administrator", Description = "Administrators can do anything. Add, edit, delete, etc.", IsBuiltIn = true },
                new Role { Id = 2, Name = "Member", Description = "Read-only user with a profile.", IsBuiltIn = true },
                new Role { Id = 3, Name = "Contributor", Description = "Contributor can add and edit unprotected pages.", IsBuiltIn = true },
                new Role { Id = 4, Name = "Moderator", Description = "Moderators can add, edit, and delete pages - including protected pages.", IsBuiltIn = true },
                new Role { Id = 5, Name = "Anonymous", Description = "Role applied to users who are not logged in.", IsBuiltIn = true },
            };

            db.Roles.AddRange(roles);
            db.SaveChanges();
            logger?.LogInformation($"DatabaseSeeder: Seeded {roles.Length} Roles.");
        }

        private static void SeedPermissions(WikiDbContext db, ILogger? logger)
        {
            if (db.Permissions.Any())
            {
                logger?.LogInformation("DatabaseSeeder: Permissions already seeded, skipping.");
                return;
            }

            logger?.LogInformation("DatabaseSeeder: Seeding Permissions...");
            var permissions = new[]
            {
                new Permission { Id = 1, Name = "Read", Description = "User or role can read page or within namespace." },
                new Permission { Id = 2, Name = "Edit", Description = "User or role can edit page or within namespace." },
                new Permission { Id = 3, Name = "Delete", Description = "User or role can delete page or within namespace." },
                new Permission { Id = 4, Name = "Moderate", Description = "User or role can moderate page or within namespace, such as editing protected pages and reverting changes." },
                new Permission { Id = 5, Name = "Create", Description = "User or role can create pages." },
            };

            db.Permissions.AddRange(permissions);
            db.SaveChanges();
            logger?.LogInformation($"DatabaseSeeder: Seeded {permissions.Length} Permissions.");
        }

        private static void SeedRolePermissions(WikiDbContext db, ILogger? logger)
        {
            if (db.RolePermissions.Any())
            {
                logger?.LogInformation("DatabaseSeeder: RolePermissions already seeded, skipping.");
                return;
            }

            logger?.LogInformation("DatabaseSeeder: Seeding RolePermissions...");
            var rolePermissions = new List<RolePermissionEntity>();

            // Administrator - all permissions on all pages and namespaces
            foreach (var permId in new[] { 1, 2, 3, 4, 5 })
            {
                rolePermissions.Add(new RolePermissionEntity { RoleId = 1, PermissionId = permId, PageId = "*", Namespace = null, PermissionDispositionId = 1 });
                rolePermissions.Add(new RolePermissionEntity { RoleId = 1, PermissionId = permId, PageId = null, Namespace = "*", PermissionDispositionId = 1 });
            }

            // Anonymous - read only
            rolePermissions.Add(new RolePermissionEntity { RoleId = 5, PermissionId = 1, PageId = "*", Namespace = null, PermissionDispositionId = 1 });
            rolePermissions.Add(new RolePermissionEntity { RoleId = 5, PermissionId = 1, PageId = null, Namespace = "*", PermissionDispositionId = 1 });

            // Member - read only
            rolePermissions.Add(new RolePermissionEntity { RoleId = 2, PermissionId = 1, PageId = "*", Namespace = null, PermissionDispositionId = 1 });
            rolePermissions.Add(new RolePermissionEntity { RoleId = 2, PermissionId = 1, PageId = null, Namespace = "*", PermissionDispositionId = 1 });

            // Contributor - read, edit, create
            foreach (var permId in new[] { 1, 2, 5 })
            {
                rolePermissions.Add(new RolePermissionEntity { RoleId = 3, PermissionId = permId, PageId = "*", Namespace = null, PermissionDispositionId = 1 });
                rolePermissions.Add(new RolePermissionEntity { RoleId = 3, PermissionId = permId, PageId = null, Namespace = "*", PermissionDispositionId = 1 });
            }

            // Moderator - read, edit, delete, moderate, create
            foreach (var permId in new[] { 1, 2, 3, 4, 5 })
            {
                rolePermissions.Add(new RolePermissionEntity { RoleId = 4, PermissionId = permId, PageId = "*", Namespace = null, PermissionDispositionId = 1 });
                rolePermissions.Add(new RolePermissionEntity { RoleId = 4, PermissionId = permId, PageId = null, Namespace = "*", PermissionDispositionId = 1 });
            }

            db.RolePermissions.AddRange(rolePermissions);
            db.SaveChanges();
            logger?.LogInformation($"DatabaseSeeder: Seeded {rolePermissions.Count} RolePermissions.");
        }

        private static void SeedConfigurationGroups(WikiDbContext db, ILogger? logger)
        {
            if (db.ConfigurationGroups.Any())
            {
                logger?.LogInformation("DatabaseSeeder: ConfigurationGroups already seeded, skipping.");
                return;
            }

            logger?.LogInformation("DatabaseSeeder: Seeding ConfigurationGroups...");
            var groups = new[]
            {
                new ConfigurationGroup { Id = 1, Name = "Basic", Description = "Basic site configuration." },
                new ConfigurationGroup { Id = 2, Name = "HTML Layout", Description = "Customizable HTML layout configuration." },
                new ConfigurationGroup { Id = 3, Name = "Functionality", Description = "Feature toggle configuration." },
                new ConfigurationGroup { Id = 4, Name = "Search", Description = "Search and indexing configuration." },
                new ConfigurationGroup { Id = 5, Name = "Performance", Description = "Performance and caching configuration." },
                new ConfigurationGroup { Id = 6, Name = "Membership", Description = "User configuration" },
                new ConfigurationGroup { Id = 7, Name = "External Authentication", Description = "External authentication providers." },
                new ConfigurationGroup { Id = 8, Name = "Cookies", Description = "Cookie configuration." },
                new ConfigurationGroup { Id = 9, Name = "Email", Description = "Email configuration." },
                new ConfigurationGroup { Id = 10, Name = "LDAP Authentication", Description = "LDAP authentication configuration." },
                new ConfigurationGroup { Id = 11, Name = "Customization", Description = "Site customization settings." },
                new ConfigurationGroup { Id = 12, Name = "Files and Attachments", Description = "File upload and attachment settings." },
            };

            db.ConfigurationGroups.AddRange(groups);
            db.SaveChanges();
            logger?.LogInformation($"DatabaseSeeder: Seeded {groups.Length} ConfigurationGroups.");
        }

        private static void SeedConfigurationEntries(WikiDbContext db, ILogger? logger)
        {
            if (db.ConfigurationEntries.Any())
            {
                logger?.LogInformation("DatabaseSeeder: ConfigurationEntries already seeded, skipping.");
                return;
            }

            logger?.LogInformation("DatabaseSeeder: Seeding ConfigurationEntries...");
            var entries = new List<ConfigurationEntry>
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

            db.ConfigurationEntries.AddRange(entries);
            db.SaveChanges();
            logger?.LogInformation($"DatabaseSeeder: Seeded {entries.Count} ConfigurationEntries.");
        }

        private static void SeedThemes(WikiDbContext db, ILogger? logger)
        {
            if (db.Themes.Any())
            {
                logger?.LogInformation("DatabaseSeeder: Themes already seeded, skipping.");
                return;
            }

            logger?.LogInformation("DatabaseSeeder: Seeding Themes...");
            var themes = new[]
            {
                new Theme { Name = "Cerulean", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/cerulean/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new Theme { Name = "Cosmo", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/cosmo/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new Theme { Name = "Cyborg", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/cyborg/bootstrap.min.css;/css/dark.css;/syntax/styles/dark.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-white", ClassDropdown = "text-white", ClassBranding = "text-white", EditorTheme = "dark" },
                new Theme { Name = "Darkly", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/darkly/bootstrap.min.css;/css/dark.css;/syntax/styles/dark.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-light", ClassDropdown = "text-light", ClassBranding = "text-light", EditorTheme = "dark" },
                new Theme { Name = "Flatly", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/flatly/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new Theme { Name = "Journal", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/journal/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new Theme { Name = "Light", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootstrap/5.3.7/css/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-light bg-light", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new Theme { Name = "Litera", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/litera/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new Theme { Name = "Lumen", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/lumen/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-light", ClassDropdown = "text-dark", ClassBranding = "text-light", EditorTheme = "light" },
                new Theme { Name = "Lux", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/lux/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-light", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new Theme { Name = "Materia", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/materia/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new Theme { Name = "Minty", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/minty/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new Theme { Name = "Morph", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/morph/bootstrap.min.css;/css/gray.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new Theme { Name = "Pulse", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/pulse/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-light", ClassDropdown = "text-dark", ClassBranding = "text-light", EditorTheme = "light" },
                new Theme { Name = "Quartz", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/quartz/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new Theme { Name = "Sandstone", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/sandstone/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-secondary", ClassDropdown = "text-primary", ClassBranding = "text-light", EditorTheme = "light" },
                new Theme { Name = "Simplex", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/simplex/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-light", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new Theme { Name = "Sketchy", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/sketchy/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-light", ClassDropdown = "text-dark", ClassBranding = "text-light", EditorTheme = "light" },
                new Theme { Name = "Slate", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/slate/bootstrap.min.css;/css/gray.css;/syntax/styles/dark.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-light", ClassDropdown = "text-light", ClassBranding = "text-light", EditorTheme = "dark" },
                new Theme { Name = "Solar", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/solar/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new Theme { Name = "Spacelab", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/spacelab/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new Theme { Name = "Superhero", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/superhero/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new Theme { Name = "United", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/united/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new Theme { Name = "Vapor", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/vapor/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new Theme { Name = "Yeti", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/yeti/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
                new Theme { Name = "Zephyr", DelimitedFiles = "https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/zephyr/bootstrap.min.css;/css/light.css;/syntax/styles/light.css", ClassNavBar = "navbar-primary bg-primary", ClassNavLink = "text-dark", ClassDropdown = "text-dark", ClassBranding = "text-dark", EditorTheme = "light" },
            };

            db.Themes.AddRange(themes);
            db.SaveChanges();
            logger?.LogInformation($"DatabaseSeeder: Seeded {themes.Length} Themes.");
        }

        private static void SeedMenuItems(WikiDbContext db, ILogger? logger)
        {
            if (db.MenuItems.Any())
            {
                logger?.LogInformation("DatabaseSeeder: MenuItems already seeded, skipping.");
                return;
            }

            logger?.LogInformation("DatabaseSeeder: Seeding MenuItems...");
            var menuItems = new[]
            {
                new MenuItem { Id = 1, Name = "Home", Link = "/Home", Ordinal = 1 },
            };

            db.MenuItems.AddRange(menuItems);
            db.SaveChanges();
            logger?.LogInformation($"DatabaseSeeder: Seeded {menuItems.Length} MenuItems.");
        }

        private static void SeedBuiltInPages(WikiDbContext db, ILogger? logger)
        {
            if (db.Pages.Any())
            {
                logger?.LogInformation("DatabaseSeeder: Pages already seeded, skipping.");
                return;
            }

            logger?.LogInformation("DatabaseSeeder: Seeding built-in Pages...");
            
            var systemUserId = Guid.Empty; // System user for built-in pages
            var now = DateTime.UtcNow;

            // Built-in pages copied from the original SQLite pages.db
            // Navigation must be lowercase to match what NamespaceNavigation.CleanAndValidate produces
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
                var page = new PageEntity
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

                var pageRevision = new PageRevisionEntity
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

                db.Pages.Add(page);
                db.PageRevisions.Add(pageRevision);
                pageId++;
            }

            db.SaveChanges();
            logger?.LogInformation($"DatabaseSeeder: Seeded {builtInPages.Length} built-in Pages.");
        }
    }
}
