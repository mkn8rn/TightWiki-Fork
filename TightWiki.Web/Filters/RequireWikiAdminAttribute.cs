using Microsoft.AspNetCore.Mvc.Filters;
using TightWiki.Contracts.Interfaces;

namespace TightWiki.Web.Filters
{
    /// <summary>
    /// Requires the current wiki session user to have administrator privileges.
    /// Throws <see cref="TightWiki.Contracts.Exceptions.UnauthorizedException"/> (caught by middleware) if not.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireWikiAdminAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.RequestServices.GetRequiredService<ISessionState>();
            session.RequireAdminPermission();
        }
    }
}
