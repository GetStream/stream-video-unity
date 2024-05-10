using System;

namespace StreamVideo.Core
{
    /// <summary>
    /// Call type defines permission settings. You can set permissions for each type in <a href="https://dashboard.getstream.io/">Stream Dashboard</a>.
    /// Read more about the call types in the <a href="https://getstream.io/video/docs/unity/guides/call-types/">Call Types Docs</a>
    /// </summary>
    public readonly struct StreamCallType
    {
        public bool IsValid => !string.IsNullOrEmpty(_callTypeKey);

        /// <summary>
        /// simple 1-1 or larger group video calling with sane defaults
        /// </summary>
        public static readonly StreamCallType Default = new StreamCallType("default");

        /// <summary>
        /// pre configured for a workflow around requesting permissions to speak
        /// </summary>
        public static StreamCallType AudioRoom => new StreamCallType("audio_room");

        /// <summary>
        /// pre configured for livestream use cases, access to calls is granted to all authenticated users
        /// </summary>
        public static StreamCallType Livestream => new StreamCallType("livestream");

        /// <summary>
        /// ** Use for development only! ** should only be used for testing, permissions are open and everything is enabled (use carefully)
        /// </summary>
        public static StreamCallType Development => new StreamCallType("development");

        public static StreamCallType Custom(string callTypeKey) => new StreamCallType(callTypeKey);

        public StreamCallType(string callTypeKey)
        {
            if (string.IsNullOrEmpty(callTypeKey))
            {
                throw new ArgumentException(
                    $"{callTypeKey} cannot be null or empty. Use predefined channel types: {nameof(Default)}, " +
                    $"{nameof(Livestream)}, {nameof(AudioRoom)}, {nameof(Development)}, or create custom one in your Dashboard and use {nameof(Custom)}");
            }

            _callTypeKey = callTypeKey;
        }

        public static implicit operator string(StreamCallType callType) => callType._callTypeKey;

        public override string ToString() => _callTypeKey;

        private readonly string _callTypeKey;
    }
}