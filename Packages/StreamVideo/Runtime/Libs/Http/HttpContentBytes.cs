using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace StreamVideo.Libs.Http
{
    /// <summary>
    /// Reads <see cref="HttpContent"/> without <see cref="HttpContent.ReadAsByteArrayAsync"/>.
    /// On WebGL that API schedules <c>ContinueWith</c> on the thread pool, which never runs, so
    /// SFU Twirp calls such as SetPublisher hang after create-call.
    /// <see cref="HttpContent.CopyToAsync"/> on <see cref="ByteArrayContent"/> completes inline,
    /// and awaiting an already-completed task stays on the Unity main thread.
    /// </summary>
    internal static class HttpContentBytes
    {
        public static async Task<byte[]> ReadAllAsync(HttpContent content)
        {
            if (content == null)
            {
                return Array.Empty<byte>();
            }

            using (var buffer = new MemoryStream())
            {
                await content.CopyToAsync(buffer);
                return buffer.ToArray();
            }
        }
    }
}
