using System;
using System.Collections.Generic;

namespace StreamVideo.Core.Web
{
    /// <summary>
    /// Requests Uri Factory
    /// </summary>
    internal interface IRequestUriFactory
    {
        Uri CreateCoordinatorConnectionUri(Func<string> clientHeaderFactory);

        Uri CreateEndpointUri(string endpoint, Dictionary<string, string> parameters = null);
    }
}