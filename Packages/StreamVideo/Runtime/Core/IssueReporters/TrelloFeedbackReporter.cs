using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using StreamVideo.Libs.Serialization;

namespace StreamVideo.Core.IssueReporters
{
    internal class TrelloFeedbackReporter : IFeedbackReporter
    {
        // StreamTODO: Remove
        public const string ApiKey = "";
        public const string Token = "";
        public const string ListId = "";

        public TrelloFeedbackReporter(ILogsProvider logsProvider, ISerializer serializer)
        {
            _serializer = serializer;
            _logsProvider = logsProvider;
        }

        public async Task SendCallReport(string callId)
        {
            var logs = _logsProvider.GetLogs();
            var zippedLogs = Zip(logs);

            using var client = new HttpClient();

            var createCardUrl = GetApiQuery("cards", $"&idList={ListId}&name={callId}");
            var createCardResponse = await client.PostAsync(createCardUrl, content: null);
            var cardResponseJson = await createCardResponse.Content.ReadAsStringAsync();
            
            var cardJObject = (JObject)_serializer.DeserializeObject(cardResponseJson);
            var id = cardJObject["id"];

            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(zippedLogs), "file", "logs.zip");

            var addCardAttachmentUrl = GetApiQuery($"cards/{id}/attachments");
            var attachmentResponse = await client.PostAsync(addCardAttachmentUrl, content);
        }

        private readonly ILogsProvider _logsProvider;
        private readonly ISerializer _serializer;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subUrl">SubURL without / prefix. E.g. "cards", $"boards/{id}"</param>
        /// <param name="paramChain">Additional URL params. E.g. $"&idList={BoardId}&name={callId}"</param>
        /// <returns>Full API Request URL with Trello domain, API Key & Token</returns>
        private static string GetApiQuery(string subUrl, string paramChain = "")
        {
            return $"https://api.trello.com/1/{subUrl}?key={ApiKey}&token={Token}{paramChain}";
        }
        
        private static byte[] Zip(string str) {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream()) {
                using (var gs = new GZipStream(mso, CompressionMode.Compress)) {
                    //msi.CopyTo(gs);
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }
        
        private static void CopyTo(Stream src, Stream dest) {
            var bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0) {
                dest.Write(bytes, 0, cnt);
            }
        }
    }
}