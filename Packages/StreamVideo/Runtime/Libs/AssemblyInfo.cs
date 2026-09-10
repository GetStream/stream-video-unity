using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("StreamVideo.Core")]

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("StreamVideo.Tests.Editor")]
#endif

#if STREAM_TESTS_ENABLED || UNITY_EDITOR
[assembly: InternalsVisibleTo("StreamVideo.Tests.Runtime")]
[assembly: InternalsVisibleTo("StreamVideo.Tests.Shared")]
#endif
