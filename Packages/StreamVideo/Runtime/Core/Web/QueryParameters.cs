using System.Collections.Generic;

namespace StreamVideo.Core.Web
{
    internal class QueryParameters : Dictionary<string, string>
    {
        public static QueryParameters Default => new QueryParameters();
    }
}