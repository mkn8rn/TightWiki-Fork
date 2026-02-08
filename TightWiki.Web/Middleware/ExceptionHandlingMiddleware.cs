using BLL.Services.Exception;
using System.Text;
using System.Text.Json;
using TightWiki.Contracts.Exceptions;
using TightWiki.Contracts;
using TightWiki.Web.Filters;

namespace TightWiki.Web.Middleware
{
    /// <summary>
    /// Catches unhandled exceptions from MVC/Razor Page actions.
    /// <para>
    /// If the matched endpoint is decorated with <see cref="ProducesViewAttribute"/>,
    /// the user is redirected to the notification page.
    /// Otherwise a JSON error response is returned.
    /// </para>
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogWarning(ex, "Unauthorized: {Message}", ex.Message);

                if (context.Response.HasStarted) return;

                if (IsViewEndpoint(context))
                {
                    context.Response.Redirect(
                        $"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString(ex.Message)}" +
                        $"&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}/")}");
                }
                else
                {
                    await WriteJsonError(context, StatusCodes.Status401Unauthorized, ex.Message);
                }
            }
            catch (Exception ex)
            {
                string request = $"{context.Request.Path}{context.Request.QueryString}";
                var routeValues = new StringBuilder();

                foreach (var rv in context.Request.RouteValues)
                {
                    routeValues.AppendLine($"{rv},");
                }
                if (routeValues.Length > 1) routeValues.Length--;

                var exceptionText = $"IP Address: {context.Connection.RemoteIpAddress},\r\n Request: {request},\r\n RouteValues: {routeValues}\r\n";

                _logger.LogError(ex, exceptionText);

                var exceptionService = context.RequestServices.GetRequiredService<IExceptionService>();
                exceptionService.LogException(ex, exceptionText);

                if (context.Response.HasStarted) return;

                if (IsViewEndpoint(context))
                {
                    context.Response.Redirect(
                        $"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString(ex.GetBaseException().Message)}");
                }
                else
                {
                    await WriteJsonError(context, StatusCodes.Status400BadRequest, ex.GetBaseException().Message);
                }
            }
        }

        /// <summary>
        /// Determines whether the matched endpoint produces a view.
        /// If no endpoint has been matched (e.g., before routing, or for static files),
        /// defaults to true so the user sees a friendly redirect rather than raw JSON.
        /// Only returns false for endpoints that have been routed to an MVC action
        /// that does NOT have [ProducesView] (i.e., API/file-serving endpoints).
        /// </summary>
        private static bool IsViewEndpoint(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint == null)
                return true; // No endpoint resolved — default to redirect behavior.

            return endpoint.Metadata.GetMetadata<ProducesViewAttribute>() != null;
        }

        private static async Task WriteJsonError(HttpContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new { success = false, errorMessage = message }));
        }
    }
}

