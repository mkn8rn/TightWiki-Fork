using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;

namespace TightWiki.Web.Bff.Extensions
{
    public static class LocalizationExtensions
    {
        public static IHtmlContent Format(this IViewLocalizer viewLocalizer, string key, params object?[] param)
            => new HtmlContentBuilder().AppendHtml(string.Format(viewLocalizer[key].Value, param));

        public static string Format(this LocalizedHtmlString localizedHtmlString, params object?[] param)
            => string.Format(localizedHtmlString.Value, param);

        public static string Format(this LocalizedString localizedString, params object?[] param)
            => string.Format(localizedString.Value, param);

        public static string Localize<T>(this IStringLocalizer<T> localizer, string key)
            => localizer[key].Value;

        public static string Localize<T>(this IStringLocalizer<T> localizer, string key, params object[] objs)
            => string.Format(localizer[key].Value, objs);
    }
}
