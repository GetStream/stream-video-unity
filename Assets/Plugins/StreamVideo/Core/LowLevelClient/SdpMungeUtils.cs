using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace StreamVideo.Core.LowLevelClient
{
    /// <summary>
    /// Utility for SDP modifications.
    /// </summary>
    internal class SdpMungeUtils
    {
        public string ModifySdp(string sdp, bool enableRed, bool enableDtx)
        {
            if (!enableRed && !enableDtx)
            {
                return sdp;
            }

            Parse(sdp);

            if (!_audioMRecordLineIndex.HasValue)
            {
                return sdp;
            }

            for (var i = 0; i < _fileBuffer.Count; i++)
            {
                var line = _fileBuffer[i];

                var opusIsDominant = _opusPosition < _redPosition;
                if (enableRed && opusIsDominant && i == _audioMRecordLineIndex)
                {
                    line = SwapOpusAndRed(line);
                }

                if (enableDtx)
                {
                    line = EnableDtx(line);
                }

                _fileSb.AppendLine(line);
            }

            return _fileSb.ToString();
        }

        private string SwapOpusAndRed(string line)
        {
            _mediaLineSb.Length = 0;
            _mediaLineSb.Append(line);

            var temp = new string('&', _opusPayloadType.Length);

            // opus -> temp
            _mediaLineSb.Replace(_opusPayloadType, temp, _opusPosition.Value, _opusPayloadType.Length);

            // red -> opus
            _mediaLineSb.Replace(_redPayloadType, _opusPayloadType, _redPosition.Value, _redPayloadType.Length);

            // temp -> red
            _mediaLineSb.Replace(temp, _redPayloadType, _opusPosition.Value, temp.Length);

            return _mediaLineSb.ToString();
        }

        private static string EnableDtx(string line) => line.EndsWith("useinbandfec=1") ? line + ";usedtx=1" : line;

        private void Parse(string sdp)
        {
            Reset();

            var reader = new StringReader(sdp);

            const string audioMRecordKey = "m=audio";
            const string opusKey = " opus/48000/2";
            const string redKey = " red/48000";
            const string ftmpRecordKey = "a=fmtp:";

            var i = 0;
            string line, redFtmpKey = string.Empty, opusFtmpKey = string.Empty;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith(audioMRecordKey))
                {
                    _audioMRecord = line;
                    _audioMRecordLineIndex = i;
                }
                else if (line.Contains(opusKey))
                {
                    ParseRtmpMapRecord(line, out _opusPayloadType);
                    opusFtmpKey = ftmpRecordKey + _opusPayloadType;
                }
                else if (line.Contains(redKey))
                {
                    ParseRtmpMapRecord(line, out _redPayloadType);
                    redFtmpKey = ftmpRecordKey + _redPayloadType;
                }
                else
                {
                    if (!string.IsNullOrEmpty(opusFtmpKey) && line.Contains(opusFtmpKey))
                    {
                        _opusFtmpRecord = line;
                        _opusFtmpRecordLineIndex = i;
                    }

                    if (!string.IsNullOrEmpty(redFtmpKey) && line.Contains(redFtmpKey))
                    {
                        _redFtmpRecord = line;
                        _redFtmpRecordLineIndex = i;
                    }
                }

                _fileBuffer.Add(line);
                i++;
            }

            if (!string.IsNullOrEmpty(_audioMRecord))
            {
                if (!string.IsNullOrEmpty(_opusPayloadType))
                {
                    _opusPosition = _audioMRecord.IndexOf(_opusPayloadType, StringComparison.Ordinal);
                }

                if (!string.IsNullOrEmpty(_redPayloadType))
                {
                    _redPosition = _audioMRecord.IndexOf(_redPayloadType, StringComparison.Ordinal);
                }
            }
        }

        private void Reset()
        {
            _fileSb.Length = 0;
            _mediaLineSb.Length = 0;

            _audioMRecord = string.Empty;
            _audioMRecordLineIndex = default;

            _opusPayloadType = default;
            _opusPosition = default;
            _opusFtmpRecord = default;
            _opusFtmpRecordLineIndex = default;

            _redPayloadType = default;
            _redPosition = default;
            _redFtmpRecord = default;
            _redFtmpRecordLineIndex = default;
        }

        private readonly StringBuilder _fileSb = new StringBuilder();
        private readonly StringBuilder _mediaLineSb = new StringBuilder();
        private readonly List<string> _fileBuffer = new List<string>();

        private string _audioMRecord;
        private int? _audioMRecordLineIndex;

        private string _opusPayloadType;
        private int? _opusPosition;
        private string _opusFtmpRecord;
        private int _opusFtmpRecordLineIndex;

        private string _redPayloadType;
        private int? _redPosition;
        private string _redFtmpRecord;
        private int _redFtmpRecordLineIndex;

        private static void ParseRtmpMapRecord(string rtmpMapLine, out string payloadType)
        {
            var trimmed = rtmpMapLine.Trim();
            var parts = trimmed.Split(" ");
            payloadType = parts[0].Split(":")[1];
        }
    }
}