//----------------------
// <auto-generated>
//     Generated using the NSwag toolchain v13.19.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0)) (http://NSwag.org)
// </auto-generated>
//----------------------


using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.InternalDTO.Events;
using StreamVideo.Core.InternalDTO.Models;

namespace StreamVideo.Core.InternalDTO.Requests
{
    using System = global::System;

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.19.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0))")]
    public partial class QueryCallsRequest
    {
        [Newtonsoft.Json.JsonProperty("filter_conditions", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.IDictionary<string, object> FilterConditions { get; set; }

        [Newtonsoft.Json.JsonProperty("limit", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int Limit { get; set; }

        [Newtonsoft.Json.JsonProperty("next", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Next { get; set; }

        [Newtonsoft.Json.JsonProperty("prev", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Prev { get; set; }

        [Newtonsoft.Json.JsonProperty("sort", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.ICollection<SortParamRequest> Sort { get; set; }

        [Newtonsoft.Json.JsonProperty("watch", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool Watch { get; set; }

    }

}

