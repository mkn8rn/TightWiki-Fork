using Autofac;
using Autofac.Extensions.DependencyInjection;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NTDLS.DelegateThreadPooling;
using TightWiki.Web.Engine;
using TightWiki.Web.Engine.Handlers;
using TightWiki.Web.Engine.Library.Interfaces;
using DAL;
using TightWiki.Utils;
using BLL.Services.Configuration;
using BLL.Services.Emojis;
using BLL.Services.Engine;
using BLL.Services.Exception;
using BLL.Services.Pages;
using BLL.Services.PageFile;
using BLL.Services.Security;
using BLL.Services.Spanned;
using BLL.Services.Statistics;
using BLL.Services.Users;

namespace DummyPageGenerator
{
    internal class Program
    {
        public class NoOpCompletionHandler : ICompletionHandler
        {
            public void Complete(ITightEngineState state)
            {
            }
        }

        public class NoOpExceptionHandler : IExceptionHandler
        {
            public void Log(ITightEngineState state, System.Exception? ex, string customText)
            {
            }
        }

        static void Main(string[] args)
        {
            SqlMapper.AddTypeHandler(new GuidTypeHandler());

            const string postgresConnectionString = "Host=localhost;Port=5432;Database=tightwiki_db;Username=postgres;Password=123456";

            var host = Host.CreateDefaultBuilder(args)
                       .ConfigureAppConfiguration((context, config) =>
                       {
                           config.SetBasePath(AppContext.BaseDirectory)
                                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                       })
                       .ConfigureServices((context, services) =>
                       {
                           // Register PostgreSQL DbContexts
                           services.AddDbContext<WikiDbContext>(options =>
                               options.UseNpgsql(postgresConnectionString));

                           services.AddDbContext<IdentityDbContext>(options =>
                               options.UseNpgsql(postgresConnectionString));

                           // Register BLL Services (same as main application)
                           services.AddScoped<IConfigurationService, ConfigurationService>();
                           services.AddScoped<IEmojiService, EmojiService>();
                           services.AddScoped<IExceptionService, ExceptionService>();
                           services.AddScoped<IPageService, PageService>();
                           services.AddScoped<IPageFileService, PageFileService>();
                           services.AddScoped<ISecurityService, SecurityService>();
                           services.AddScoped<ISpannedService, SpannedService>();
                           services.AddScoped<IStatisticsService, StatisticsService>();
                           services.AddScoped<IUsersService, UsersService>();

                           // Register IEngineDataProvider (bridge between BLL and Engine)
                           services.AddScoped<IEngineDataProvider, EngineDataProvider>();

                           // Register Identity services
                           services.AddIdentity<IdentityUser, IdentityRole>()
                                   .AddEntityFrameworkStores<IdentityDbContext>()
                                   .AddDefaultTokenProviders();

                           // Register PageGenerator
                           services.AddScoped<PageGenerator>();

                           services.AddLogging(configure => configure.AddConsole());
                       })
                       .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                       .ConfigureContainer<ContainerBuilder>(containerBuilder =>
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
                           containerBuilder.RegisterType<NoOpExceptionHandler>().As<IExceptionHandler>().SingleInstance();
                           containerBuilder.RegisterType<NoOpCompletionHandler>().As<ICompletionHandler>().SingleInstance();

                           containerBuilder.RegisterType<TightEngine>().As<ITightEngine>();
                       }).Build();

            // Load configuration into GlobalConfiguration
            using (var initScope = host.Services.CreateScope())
            {
                var configService = initScope.ServiceProvider.GetRequiredService<IConfigurationService>();
                configService.ReloadAllConfiguration();

                var emojiService = initScope.ServiceProvider.GetRequiredService<IEmojiService>();
                emojiService.ReloadEmojisCache();
            }

            var pool = new DelegateThreadPool();

            while (true)
            {
                using var scope = host.Services.CreateScope();
                var pg = scope.ServiceProvider.GetRequiredService<PageGenerator>();

                var workload = pool.CreateChildPool();

                foreach (var user in pg.Users)
                {
                    workload.Enqueue(() =>
                    {
                        using var innerScope = host.Services.CreateScope();
                        var engine = innerScope.ServiceProvider.GetRequiredService<ITightEngine>();
                        var dataProvider = innerScope.ServiceProvider.GetRequiredService<IEngineDataProvider>();
                        var innerPg = innerScope.ServiceProvider.GetRequiredService<PageGenerator>();

                        var config = new TightWiki.Contracts.EngineConfiguration
                        {
                            BasePath = TightWiki.Contracts.GlobalConfiguration.BasePath,
                            SiteName = TightWiki.Contracts.GlobalConfiguration.Name,
                            RecordCompilationMetrics = TightWiki.Contracts.GlobalConfiguration.RecordCompilationMetrics,
                            EnablePublicProfiles = TightWiki.Contracts.GlobalConfiguration.EnablePublicProfiles,
                            Emojis = TightWiki.Contracts.GlobalConfiguration.Emojis
                        };

                        //Create a new page:
                        innerPg.GeneratePage(engine, config, dataProvider, user.UserId);

                        //Modify existing pages:
                        int modifications = innerPg.Random.Next(0, 10);
                        for (int i = 0; i < modifications; i++)
                        {
                            innerPg.ModifyRandomPages(engine, config, dataProvider, user.UserId);
                        }
                    });
                }

                workload.WaitForCompletion();
            }
        }
    }
}
