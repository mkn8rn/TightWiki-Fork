using Microsoft.AspNetCore.Http;
using NTDLS.Helpers;

namespace TightWiki.Web.Bff.Extensions
{
    public static class HttpRequestExtensions
    {
        public static V? GetQueryValue<V>(this HttpRequest request, string key)
        {
            if (request.Query.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
            {
                return Converters.ConvertToNullable<V>(value);
            }
            return default;
        }

        public static V GetQueryValue<V>(this HttpRequest request, string key, V defaultValue)
        {
            if (request.Query.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
            {
                return Converters.ConvertToNullable<V>(value) ?? defaultValue;
            }
            return defaultValue;
        }

        public static string? GetFormValue(this HttpRequest request, string key)
            => request.Form[key];

        public static string GetFormValue(this HttpRequest request, string key, string defaultValue)
            => (string?)request.Form[key] ?? defaultValue;

        public static int GetFormValue(this HttpRequest request, string key, int defaultValue)
            => int.Parse(request.GetFormValue(key, defaultValue.ToString()));
    }
}
