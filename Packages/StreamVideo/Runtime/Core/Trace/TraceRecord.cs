using System;

namespace StreamVideo.Core.Trace
{
    /// <summary>
    /// A single trace item captured by Tracer.
    /// </summary>
    internal struct TraceRecord
    {
        /// <summary>
        /// A short identifier that categorizes the trace entry.
        /// </summary>
        public string Tag;
        
        /// <summary>
        /// Optional identifier propagated from the owning Tracer.
        /// </summary>
        public string Id;
        
        /// <summary>
        /// Arbitrary payload supplied by the caller.
        /// </summary>
        public object Data;
        
        /// <summary>
        /// Epoch-milliseconds when the entry was recorded.
        /// </summary>
        public long Timestamp;

        public TraceRecord(string tag, string id, object data, long timestamp)
        {
            Tag = tag;
            Id = id;
            Data = data;
            Timestamp = timestamp;
        }
    }
}

