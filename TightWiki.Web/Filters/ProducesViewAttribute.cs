using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TightWiki.Web.Filters
{
    /// <summary>
    /// Marks an action as producing a Razor view.
    /// <para>
    /// When the action returns a ViewModel (any non-<see cref="IActionResult"/> object),
    /// this filter automatically wraps it in a <see cref="ViewResult"/>.
    /// </para>
    /// <para>
    /// Also signals to <see cref="Middleware.ExceptionHandlingMiddleware"/> that unhandled
    /// exceptions should redirect to the notification page rather than returning a JSON error.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ProducesViewAttribute : ResultFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is ObjectResult objectResult
                && context.Controller is Controller controller)
            {
                controller.ViewData.Model = objectResult.Value;
                context.Result = new ViewResult
                {
                    ViewName = null,
                    ViewData = controller.ViewData,
                    TempData = controller.TempData,
                };
            }
        }
    }
}
