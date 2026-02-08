using System.Threading.RateLimiting;
using BLL.Services.Configuration;
using BLL.Services.Emojis;
using BLL.Services.Exception;
using BLL.Services.Pages;
using BLL.Services.PageFile;
using BLL.Services.Statistics;
using BLL.Services.Users;
using DAL;
using Microsoft.AspNetCore.RateLimiting;
using TightWiki.API.Services;

var builder = WebApplication.CreateBuilder(args);

// EF Core contexts — centralized in Infrastructure.
builder.Services.AddTightWikiDbContexts();

// BLL services resolved per-scope inside background jobs.
builder.Services.AddScoped<IPageService, PageService>();
builder.Services.AddScoped<IPageFileService, PageFileService>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IEmojiService, EmojiService>();
builder.Services.AddScoped<IExceptionService, ExceptionService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

// Async job infrastructure.
builder.Services.AddSingleton<BackgroundJobService>();

// Rate limiting.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // General API: 6 requests per minute per IP.
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = 6;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    // Export endpoint: 2 requests per hour per IP.
    options.AddFixedWindowLimiter("export", limiterOptions =>
    {
        limiterOptions.PermitLimit = 2;
        limiterOptions.Window = TimeSpan.FromHours(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            """{"error":"Rate limit exceeded. Please try again later."}""",
            cancellationToken);
    };
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "TightWiki API");
});

app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();
