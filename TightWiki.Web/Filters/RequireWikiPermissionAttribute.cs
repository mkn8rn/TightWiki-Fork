using Microsoft.AspNetCore.Mvc.Filters;
using TightWiki.Contracts.Interfaces;
using static TightWiki.Contracts.Constants;

namespace TightWiki.Web.Filters
{
    /// <summary>
    /// Requires the current wiki session user to hold one or more wiki permissions.
    /// Throws <see cref="TightWiki.Contracts.Exceptions.UnauthorizedException"/> (caught by middleware) if not.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireWikiPermissionAttribute : ActionFilterAttribute
    {
        private readonly WikiPermission[] _permissions;

        public RequireWikiPermissionAttribute(params WikiPermission[] permissions)
        {
            _permissions = permissions;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.RequestServices.GetRequiredService<ISessionState>();

            foreach (var permission in _permissions)
            {
                session.RequirePermission(null, permission);
            }
        }
    }
}
