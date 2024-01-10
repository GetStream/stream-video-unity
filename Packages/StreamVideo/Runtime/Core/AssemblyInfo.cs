#if UNITY_EDITOR || STREAM_TESTS_ENABLED
using System.Runtime.CompilerServices;
#endif

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("StreamVideo.EditorTools")]
[assembly: InternalsVisibleTo("StreamVideo.Tests.Editor")]
#endif

#if STREAM_TESTS_ENABLED || UNITY_EDITOR
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("StreamVideo.Tests.Runtime")]
[assembly: InternalsVisibleTo("StreamVideo.Tests.Shared")]
#endif