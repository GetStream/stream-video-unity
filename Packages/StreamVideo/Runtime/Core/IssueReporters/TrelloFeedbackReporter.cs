using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Device;

namespace StreamVideo.Core.IssueReporters
{
    internal class TrelloFeedbackReporter : IFeedbackReporter
    {
        // StreamTODO: Move to local config
        public const string SendReportEndpoint = "http://194.59.158.13:3000/send-logs";

        public TrelloFeedbackReporter(ILogsProvider logsProvider)
        {
            _logsProvider = logsProvider;
        }

        public async Task SendCallReport(string callId, string participantId)
        {
            using var client = new HttpClient();

            var deviceLogs = _logsProvider.GetLogs();
            var zippedLogs = Zip(deviceLogs);
            var logsFileName = GetLogsFileName(participantId);

            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(zippedLogs), "file", $"{logsFileName}.zip");
            content.Add(new StringContent(callId), "callId");

            var response = await client.PostAsync(SendReportEndpoint, content);
        }

        private readonly ILogsProvider _logsProvider;

        private string GetLogsFileName(string participantId)
        {
            var platform = Application.platform;
            var version = Application.version;
            var model = SystemInfo.deviceModel != SystemInfo.unsupportedIdentifier ? SystemInfo.deviceModel : "unknown";
            var name = SystemInfo.deviceName;
            
            return $"logs_{participantId}_{platform}_{version}_{model}_{name}_.zip";
        }

        private static byte[] Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        private static void CopyTo(Stream src, Stream dest)
        {
            var bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }
    }
}