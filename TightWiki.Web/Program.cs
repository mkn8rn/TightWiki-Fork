using Autofac;
using Autofac.Extensions.DependencyInjection;
using Dapper;
using DiffPlex;
using DiffPlex.DiffBuilder;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NTDLS.Helpers;
using TightWiki.Contracts;
using TightWiki.Contracts.Interfaces;
using TightWiki.Web.Engine;
using TightWiki.Web.Engine.Handlers;
using TightWiki.Web.Engine.Library.Interfaces;
using DAL;
using TightWiki.Helpers;
using TightWiki.Localisation;
using TightWiki.Static;
using TightWiki.Utils;
using BLL.Services.Configuration;
using BLL.Services.Email;
using BLL.Services.Emojis;
using BLL.Services.Engine;
using BLL.Services.Exception;
using BLL.Services.Pages;
using BLL.Services.PageFile;
using BLL.Services.Security;
using BLL.Services.Seeding;
using BLL.Services.Spanned;
using BLL.Services.Statistics;
using BLL.Services.Users;


namespace TightWiki
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddTightWikiDbContexts();

            // Register BLL Services
            builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
            builder.Services.AddScoped<IDatabaseSeedingService, DatabaseSeedingService>();
            builder.Services.AddScoped<IEmojiService, EmojiService>();
            builder.Services.AddScoped<IExceptionService, ExceptionService>();
            builder.Services.AddScoped<IPageService, PageService>();
            builder.Services.AddScoped<IPageFileService, PageFileService>();
            builder.Services.AddScoped<ISecurityService, SecurityService>();
            builder.Services.AddScoped<ISpannedService, SpannedService>();
            builder.Services.AddScoped<IStatisticsService, StatisticsService>();
            builder.Services.AddScoped<IUsersService, UsersService>();
            builder.Services.AddScoped<IEngineConfigurationProvider, EngineConfigurationProvider>();

            // Register IEngineDataProvider (bridge between BLL services and Engine layer)
            builder.Services.AddScoped<IEngineDataProvider, EngineDataProvider>();

            // SQLite specific initialization. Disabled until repositories are migrated to Postgres.


            // ManagedDataStorage.Pages.SetConnectionString(builder.Configuration.GetConnectionString("PagesConnection"));
            // ManagedDataStorage.DeletedPages.SetConnectionString(builder.Configuration.GetConnectionString("DeletedPagesConnection"));
            // ManagedDataStorage.DeletedPageRevisions.SetConnectionString(builder.Configuration.GetConnectionString("DeletedPageRevisionsConnection"));
            // ManagedDataStorage.Statistics.SetConnectionString(builder.Configuration.GetConnectionString("StatisticsConnection"));
            // ManagedDataStorage.Emoji.SetConnectionString(builder.Configuration.GetConnectionString("EmojiConnection"));
            // ManagedDataStorage.Exceptions.SetConnectionString(builder.Configuration.GetConnectionString("ExceptionsConnection"));
            // ManagedDataStorage.Users.SetConnectionString(builder.Configuration.GetConnectionString("UsersConnection"));
            // ManagedDataStorage.Config.SetConnectionString(builder.Configuration.GetConnectionString("ConfigConnection"));
            //
            // DatabaseUpgrade.UpgradeDatabase();
            // ConfigurationRepository.ReloadEverything();

            // Add DiffPlex services.
            builder.Services.AddScoped<IDiffer, Differ>();
            builder.Services.AddScoped<ISideBySideDiffBuilder>(sp =>
                new SideBySideDiffBuilder(sp.GetRequiredService<IDiffer>()));

            var requireConfirmedAccount = false;

            // Add services to the container.
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddLocalization(opts => { opts.ResourcesPath = "Resources"; });

            // Adds support for controllers and views
            builder.Services.AddControllersWithViews(config =>
                {
                    config.ModelBinderProviders.Insert(0, new InvariantDecimalModelBinderProvider());
                })
                .AddViewLocalization(
                    LanguageViewLocationExpanderFormat.Suffix,
                    opts =>
                    {
                        opts.ResourcesPath = "Resources";
                    })
                .AddDataAnnotationsLocalization()
                .AddXmlSerializerFormatters()
                .AddXmlDataContractSerializerFormatters();

            builder.Services.AddRazorPages()
                .AddViewLocalization(
                    LanguageViewLocationExpanderFormat.Suffix,
                    opts =>
                    {
                        opts.ResourcesPath = "Resources";
                    });

            var supportedCultures = new SupportedCultures();
            builder.Services.AddSingleton(x => supportedCultures);

            builder.Services.Configure<RequestLocalizationOptions>(opts =>
            {
                opts.DefaultRequestCulture = new RequestCulture("en");
                // Formatting numbers, dates, etc.
                opts.SupportedCultures = supportedCultures.UICompleteCultures;
                // UI strings that we have localized.
                opts.SupportedUICultures = supportedCultures.UICompleteCultures;

                opts.RequestCultureProviders = new List<IRequestCultureProvider>
                {
                    //new Routing.LanguageRouteRequestCultureProvider(supportedCultures),
                    new QueryStringRequestCultureProvider(),
                    new CookieRequestCultureProvider(),
                    new AcceptLanguageHeaderRequestCultureProvider(),
                };
            });
            builder.Services.AddSingleton<RequestLocalizationOptions>();

            //builder.Services.Configure<RouteOptions>(options =>
            //{
            //    options.ConstraintMap.Add("lang", typeof(LanguageRouteConstraint));
            //});


            builder.Services.AddSingleton<IEmailService, EmailService>();

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = requireConfirmedAccount)
                .AddEntityFrameworkStores<IdentityDbContext>();

            // Build a temporary service provider to access configuration during startup
            var tempServiceProvider = builder.Services.BuildServiceProvider();
            var configurationService = tempServiceProvider.GetRequiredService<IConfigurationService>();

            var externalAuthenticationConfig = configurationService.GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.ExternalAuthentication);
            var basicConfig = configurationService.GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.Basic);
            var cookiesConfig = configurationService.GetConfigurationEntriesByGroupName(Constants.ConfigurationGroup.Cookies);

            var authentication = builder.Services.AddAuthentication()
                .AddCookie("CookieAuth", options =>
                {
                    options.Cookie.Name = basicConfig.Value<string>("Name").EnsureNotNull();
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.LoginPath = $"{GlobalConfiguration.BasePath}/Identity/Account/Login";
                    options.ExpireTimeSpan = TimeSpan.FromHours(cookiesConfig.Value<int>("Expiration Hours"));
                    options.SlidingExpiration = true;
                    options.Cookie.IsEssential = true;

                });


            var persistKeysPath = cookiesConfig.Value("Persist Keys Path", string.Empty);
            if (string.IsNullOrEmpty(persistKeysPath) == false)
            {
                if (CanReadWrite(persistKeysPath))
                {
                    // Add persistent data protection
                    builder.Services.AddDataProtection()
                        .PersistKeysToFileSystem(new DirectoryInfo(persistKeysPath))
                        .SetApplicationName(basicConfig.Value<string>("Name").EnsureNotNull());
                }
                else
                {
                    // Log warning - can't use exception service during startup configuration
                    Console.WriteLine($"WARNING: Cannot read/write to the specified path for persistent keys: {persistKeysPath}. Check the configuration and path permission.");
                }
            }

            if (externalAuthenticationConfig.Value<bool>("Google : Use Google Authentication"))
            {
                var clientId = externalAuthenticationConfig.Value<string>("Google : ClientId");
                var clientSecret = externalAuthenticationConfig.Value<string>("Google : ClientSecret");

                if (clientId != null && clientSecret != null && !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
                {
                    authentication.AddGoogle(options =>
                    {
                        options.ClientId = clientId;
                        options.ClientSecret = clientSecret;

                        options.Events = new OAuthEvents
                        {
                            OnRemoteFailure = context =>
                            {
                                context.Response.Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString("External login was canceled.")}");
                                context.HandleResponse();
                                return Task.CompletedTask;
                            }
                        };
                    });
                }
            }
            if (externalAuthenticationConfig.Value<bool>("Microsoft : Use Microsoft Authentication"))
            {
                var clientId = externalAuthenticationConfig.Value<string>("Microsoft : ClientId");
                var clientSecret = externalAuthenticationConfig.Value<string>("Microsoft : ClientSecret");

                if (clientId != null && clientSecret != null && !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
                {
                    authentication.AddMicrosoftAccount(options =>
                    {
                        options.ClientId = clientId;
                        options.ClientSecret = clientSecret;

                        options.Events = new OAuthEvents
                        {
                            OnRemoteFailure = context =>
                            {
                                context.Response.Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString("External login was canceled.")}");
                                context.HandleResponse();
                                return Task.CompletedTask;
                            }
                        };

                    });
                }
            }

            if (externalAuthenticationConfig.Value<bool>("OIDC : Use OIDC Authentication"))
            {
                var authority = externalAuthenticationConfig.Value<string>("OIDC : Authority");
                var clientId = externalAuthenticationConfig.Value<string>("OIDC : ClientId");
                var clientSecret = externalAuthenticationConfig.Value<string>("OIDC : ClientSecret");

                if (!string.IsNullOrEmpty(authority) && !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
                {
                    authentication.AddOpenIdConnect("oidc", options =>
                    {
                        options.Authority = authority;
                        options.ClientId = clientId;
                        options.ClientSecret = clientSecret;
                        options.ResponseType = "code";

                        options.SaveTokens = true;
                        options.GetClaimsFromUserInfoEndpoint = true;

                        options.Scope.Clear();
                        options.Scope.Add("openid");
                        options.Scope.Add("profile");
                        options.Scope.Add("email");

                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            NameClaimType = "name",
                            RoleClaimType = "role"
                        };

                        options.Events = new OpenIdConnectEvents
                        {
                            OnRemoteFailure = context =>
                            {
                                context.Response.Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString("OIDC login was canceled.")}");
                                context.HandleResponse();
                                return Task.CompletedTask;
                            }
                        };
                    });
                }
            }

            builder.Services.AddControllersWithViews();

            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
            {
                containerBuilder.RegisterType<StandardFunctionHandler>().As<IStandardFunctionHandler>().SingleInstance();
                containerBuilder.RegisterType<ScopeFunctionHandler>().As<IScopeFunctionHandler>().SingleInstance();
                containerBuilder.RegisterType<ProcessingInstructionFunctionHandler>().As<IProcessingInstructionFunctionHandler>().SingleInstance();
                containerBuilder.RegisterType<PostProcessingFunctionHandler>().As<IPostProcessingFunctionHandler>().SingleInstance();
                containerBuilder.RegisterType<MarkupHandler>().As<IMarkupHandler>().SingleInstance();
                containerBuilder.RegisterType<HeadingHandler>().As<IHeadingHandler>().SingleInstance();
                containerBuilder.RegisterType<CommentHandler>().As<ICommentHandler>().SingleInstance();
                containerBuilder.RegisterType<EmojiHandler>().As<IEmojiHandler>().SingleInstance();
                containerBuilder.RegisterType<ExternalLinkHandler>().As<IExternalLinkHandler>().SingleInstance();
                containerBuilder.RegisterType<InternalLinkHandler>().As<IInternalLinkHandler>().SingleInstance();
                containerBuilder.RegisterType<ExceptionHandler>().As<IExceptionHandler>().InstancePerLifetimeScope();
                containerBuilder.RegisterType<CompletionHandler>().As<ICompletionHandler>().InstancePerLifetimeScope();

                containerBuilder.RegisterType<TightEngine>().As<ITightEngine>().SingleInstance();
            });

            var basePath = builder.Configuration.GetValue<string>("BasePath");
            if (!string.IsNullOrEmpty(basePath))
            {
                GlobalConfiguration.BasePath = basePath;

                builder.Services.ConfigureApplicationCookie(options =>
                {
                    if (!string.IsNullOrEmpty(basePath))
                    {
                        options.LoginPath = new PathString($"{basePath}/Identity/Account/Login");
                        options.LogoutPath = new PathString($"{basePath}/Identity/Account/Logout");
                        options.AccessDeniedPath = new PathString($"{basePath}/Identity/Account/AccessDenied");
                        options.Cookie.Path = basePath; // Ensure the cookie is scoped to the sub-site path.
                    }
                    else
                    {
                        options.LoginPath = new PathString("/Identity/Account/Login");
                        options.LogoutPath = new PathString("/Identity/Account/Logout");
                        options.AccessDeniedPath = new PathString("/Identity/Account/AccessDenied");
                        options.Cookie.Path = "/"; // Use root path if no base path is set.
                    }
                });
            }



            var app = builder.Build();

            // TEMPORARY HACK: Wipe and recreate the database on every startup for development.
            // Remove this block when database schema is stable!
            using (var scope = app.Services.CreateScope())
            {
                var wikiDb = scope.ServiceProvider.GetRequiredService<WikiDbContext>();
                var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                logger.LogWarning("?? DEVELOPMENT MODE: Wiping database on startup...");
                
                // Delete the database once (both contexts share the same database)
                wikiDb.Database.EnsureDeleted();
                
                // Create tables for WikiDbContext
                wikiDb.Database.EnsureCreated();
                
                // EnsureCreated() won't create IdentityDbContext tables because the database already exists.
                // Use raw SQL to create the Identity tables using the model's generated SQL.
                var identityRelationalDb = identityDb.Database;
                var identitySql = identityRelationalDb.GenerateCreateScript();
                identityRelationalDb.ExecuteSqlRaw(identitySql);
                
                logger.LogWarning("?? DEVELOPMENT MODE: Database wiped and recreated.");
            }

            // Seed the database with initial data (themes, configuration, etc.)
            using (var scope = app.Services.CreateScope())
            {
                var seedingService = scope.ServiceProvider.GetRequiredService<IDatabaseSeedingService>();
                seedingService.SeedDatabase();
            }

            // Reload all configuration after seeding to ensure GlobalConfiguration is populated.
            // This must happen after seeding so that themes, menu items, and other config values are available.
            using (var scope = app.Services.CreateScope())
            {
                var configService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
                configService.ReloadAllConfiguration();
            }

            // Reload emoji cache using the BLL service
            using (var scope = app.Services.CreateScope())
            {
                var emojiService = scope.ServiceProvider.GetRequiredService<IEmojiService>();
                emojiService.ReloadEmojisCache();
            }

            //Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }


            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            if (!string.IsNullOrEmpty(basePath))
            {
                app.UsePathBase(basePath);

                // Redirect root requests to basePath (something like '/TightWiki').
                app.Use(async (context, next) =>
                {
                    if (context.Request.Path == "/")
                    {
                        context.Response.Redirect(basePath);
                        return;
                    }
                    await next();
                });

                app.UseStaticFiles(new StaticFileOptions
                {
                    OnPrepareResponse = ctx =>
                    {
                        ctx.Context.Request.PathBase = basePath;
                    }
                });
            }

            // Global localization providers.
            var localizer = app.Services.GetRequiredService<IStringLocalizer<StaticLocalizer>>();
            StaticLocalizer.Initialize(localizer);
            PageSelectorGenerator.Initialize(localizer);

            app.UseRouting();

            app.UseAuthentication(); // Ensures the authentication middleware is configured
            app.UseAuthorization();

            using (var scope = app.Services.CreateScope())
            {
                var options = scope.ServiceProvider.GetService<IOptions<RequestLocalizationOptions>>();
                if (options != null)
                    app.UseRequestLocalization(options.Value);
            }

            app.MapRazorPages();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Page}/{action=Display}");

            app.MapControllerRoute(
                name: "Page_Edit",
                pattern: "Page/{givenCanonical}/Edit");

            //
            // to do language route

            // Validate encryption and create admin user if needed.
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                    var securityService = services.GetRequiredService<ISecurityService>();
                    await securityService.ValidateEncryptionAndCreateAdminUserAsync(userManager);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            app.Run();
        }
        private static bool CanReadWrite(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                string tempFilePath = Path.Combine(path, Path.GetRandomFileName());
                File.WriteAllText(tempFilePath, "test");
                File.Delete(tempFilePath);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}

