//----------------------
// <auto-generated>
//     Generated using the NSwag toolchain v13.19.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0)) (http://NSwag.org)
// </auto-generated>
//----------------------


using StreamVideo.Core.InternalDTO.Requests;
using StreamVideo.Core.InternalDTO.Responses;
using StreamVideo.Core.InternalDTO.Models;

namespace StreamVideo.Core.InternalDTO.Events
{
    using System = global::System;

    /// <summary>
    /// This event is sent when a participant leaves a call session
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "13.19.0.0 (NJsonSchema v10.9.0.0 (Newtonsoft.Json v10.0.0.0))")]
    public partial class CallSessionParticipantLeftEvent
    {
        [Newtonsoft.Json.JsonProperty("call_cid", Required = Newtonsoft.Json.Required.Always)]
        public string CallCid { get; set; }

        [Newtonsoft.Json.JsonProperty("created_at", Required = Newtonsoft.Json.Required.Always)]
        public System.DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Call session ID
        /// </summary>
        [Newtonsoft.Json.JsonProperty("session_id", Required = Newtonsoft.Json.Required.Always)]
        public string SessionId { get; set; }

        /// <summary>
        /// The type of event: "call.session_participant_left" in this case
        /// </summary>
        [Newtonsoft.Json.JsonProperty("type", Required = Newtonsoft.Json.Required.Always)]
        public string Type { get; set; } = "call.session_participant_left";

        /// <summary>
        /// The user that left the call session
        /// </summary>
        [Newtonsoft.Json.JsonProperty("user", Required = Newtonsoft.Json.Required.Always)]
        public UserResponse User { get; set; } = new UserResponse();

        /// <summary>
        /// The user session ID of the user that left the call session
        /// </summary>
        [Newtonsoft.Json.JsonProperty("user_session_id", Required = Newtonsoft.Json.Required.Always)]
        public string UserSessionId { get; set; }

    }

}

