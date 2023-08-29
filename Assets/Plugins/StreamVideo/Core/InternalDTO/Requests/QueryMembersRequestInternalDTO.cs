//----------------------
// <auto-generated>
//     Generated using the NSwag toolchain v13.20.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0)) (http://NSwag.org)
// </auto-generated>
//----------------------

#nullable enable


using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.InternalDTO.Events;
using StreamVideo.Core.InternalDTO.Models;

namespace StreamVideo.Core.InternalDTO.Requests
{
    using System = global::System;

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.20.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0))")]
    internal partial class QueryMembersRequestInternalDTO
    {
        [Newtonsoft.Json.JsonProperty("filter_conditions", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.Dictionary<string, object> FilterConditions { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("id", Required = Newtonsoft.Json.Required.Always)]
        public string Id { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("limit", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int Limit { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("next", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Next { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("prev", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Prev { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("sort", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Collections.Generic.List<SortParamRequestInternalDTO?> Sort { get; set; } = default!;

        [Newtonsoft.Json.JsonProperty("type", Required = Newtonsoft.Json.Required.Always)]
        public string Type { get; set; } = default!;

    }

}

