using System;

namespace StreamVideo.Core.Web
{
    internal static class QueryParametersExt
    {
        public static QueryParameters Set(this QueryParameters queryParameters, string key, bool value)
            => Set(queryParameters, key, value.ToString());

        public static QueryParameters Set(this QueryParameters queryParameters, string key, string value)
        {
            queryParameters[key] = Uri.EscapeDataString(value);
            return queryParameters;
        }

        public static QueryParameters AppendFrom(this QueryParameters queryParameters, IAppendableQueryParameters source)
        {
            source.AppendQueryParameters(queryParameters);
            return queryParameters;
        }
    }
}