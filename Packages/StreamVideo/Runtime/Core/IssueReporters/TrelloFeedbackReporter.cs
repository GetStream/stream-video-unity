using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Device;

namespace StreamVideo.Core.IssueReporters
{
#if STREAM_DEBUG_ENABLED
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
            var logsFileName = GetLogsFileName(participantId);
            var zippedLogs = CreateZipArchive(deviceLogs, logsFileName);

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

            return $"logs_{participantId}_{platform}_{version}_{model}_{name}";
        }

        private static byte[] CreateZipArchive(string deviceLogs, string logsFileName)
        {
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = archive.CreateEntry(logsFileName + ".txt", CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream);
                writer.Write(deviceLogs);
            }

            return memoryStream.ToArray();
        }
    }
#endif
}