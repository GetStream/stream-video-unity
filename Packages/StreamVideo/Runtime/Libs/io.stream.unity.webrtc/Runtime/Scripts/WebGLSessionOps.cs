using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace Unity.WebRTC
{
#if UNITY_WEBGL && !UNITY_EDITOR
    internal static class WebGLSessionOps
    {
        static readonly Dictionary<IntPtr, Queue<CreateSessionDescriptionObserver>> s_create
            = new Dictionary<IntPtr, Queue<CreateSessionDescriptionObserver>>();

        static readonly Dictionary<IntPtr, Queue<SetSessionDescriptionObserver>> s_set
            = new Dictionary<IntPtr, Queue<SetSessionDescriptionObserver>>();

        static int s_nextHandle;

        public static CreateSessionDescriptionObserver EnqueueCreate(IntPtr peer)
        {
            var observer = CreateSessionDescriptionObserver.CreateWithHandle(NextHandle());
            Enqueue(s_create, peer, observer);
            return observer;
        }

        public static SetSessionDescriptionObserver EnqueueSet(IntPtr peer)
        {
            var observer = SetSessionDescriptionObserver.CreateWithHandle(NextHandle());
            Enqueue(s_set, peer, observer);
            return observer;
        }

        public static bool TryDequeueCreate(IntPtr peer, out CreateSessionDescriptionObserver observer)
            => TryDequeue(s_create, peer, out observer);

        public static bool TryDequeueSet(IntPtr peer, out SetSessionDescriptionObserver observer)
            => TryDequeue(s_set, peer, out observer);

        public static IntPtr[] PtrToIntPtrArray(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return Array.Empty<IntPtr>();

            int len = Marshal.ReadInt32(ptr);
            if (len <= 0)
                return Array.Empty<IntPtr>();

            var values = new int[len];
            Marshal.Copy(IntPtr.Add(ptr, 4), values, 0, len);
            var result = new IntPtr[len];
            for (int i = 0; i < len; i++)
                result[i] = new IntPtr(values[i]);
            return result;
        }

        [AOT.MonoPInvokeCallback(typeof(DelegateWebGLCreateSessionSuccess))]
        public static void OnCreateSuccess(IntPtr peerPtr, int sdpType, string sdp)
        {
            WebRTC.Sync(peerPtr, () =>
            {
                if (!TryDequeueCreate(peerPtr, out var observer))
                    return;
                observer.Invoke((RTCSdpType)sdpType, sdp, RTCErrorType.None, null);
            });
        }

        [AOT.MonoPInvokeCallback(typeof(DelegateWebGLCreateSessionFailure))]
        public static void OnCreateFailure(IntPtr peerPtr, int errorType, string message)
        {
            WebRTC.Sync(peerPtr, () =>
            {
                if (!TryDequeueCreate(peerPtr, out var observer))
                    return;
                observer.Invoke(RTCSdpType.Offer, null, (RTCErrorType)errorType, message);
            });
        }

        [AOT.MonoPInvokeCallback(typeof(DelegateWebGLSetSessionSuccess))]
        public static void OnSetSuccess(IntPtr peerPtr)
        {
            WebRTC.Sync(peerPtr, () =>
            {
                if (!TryDequeueSet(peerPtr, out var observer))
                    return;
                observer.Invoke(RTCErrorType.None, null);
            });
        }

        [AOT.MonoPInvokeCallback(typeof(DelegateWebGLSetSessionFailure))]
        public static void OnSetFailure(IntPtr peerPtr, int errorType, string message)
        {
            WebRTC.Sync(peerPtr, () =>
            {
                if (!TryDequeueSet(peerPtr, out var observer))
                    return;
                observer.Invoke((RTCErrorType)errorType, message);
            });
        }

        static IntPtr NextHandle()
            => new IntPtr(Interlocked.Increment(ref s_nextHandle));

        static void Enqueue<T>(Dictionary<IntPtr, Queue<T>> map, IntPtr key, T value)
        {
            if (!map.TryGetValue(key, out var queue))
            {
                queue = new Queue<T>();
                map[key] = queue;
            }

            queue.Enqueue(value);
        }

        static bool TryDequeue<T>(Dictionary<IntPtr, Queue<T>> map, IntPtr key, out T value)
        {
            value = default;
            if (!map.TryGetValue(key, out var queue) || queue.Count == 0)
                return false;
            value = queue.Dequeue();
            return true;
        }
    }

    internal static class WebGLRtpCapabilitiesParser
    {
        public static RTCRtpCapabilities Parse(string json)
        {
            if (string.IsNullOrEmpty(json) || json == "null")
                return new RTCRtpCapabilities(Array.Empty<RTCRtpCodecCapability>(), Array.Empty<RTCRtpHeaderExtensionCapability>());

            var dto = JsonUtility.FromJson<WebGLRtpCapabilitiesDto>(json);
            var codecs = Array.Empty<RTCRtpCodecCapability>();
            if (dto?.codecs != null)
            {
                codecs = new RTCRtpCodecCapability[dto.codecs.Length];
                for (int i = 0; i < dto.codecs.Length; i++)
                    codecs[i] = ToCodec(dto.codecs[i]);
            }

            var extensions = Array.Empty<RTCRtpHeaderExtensionCapability>();
            if (dto?.headerExtensions != null)
            {
                extensions = new RTCRtpHeaderExtensionCapability[dto.headerExtensions.Length];
                for (int i = 0; i < dto.headerExtensions.Length; i++)
                    extensions[i] = new RTCRtpHeaderExtensionCapability { uri = dto.headerExtensions[i].uri };
            }

            return new RTCRtpCapabilities(codecs, extensions);
        }

        public static RTCRtpSendParameters ParseSendParameters(string json)
        {
            if (string.IsNullOrEmpty(json) || json == "null")
                return new RTCRtpSendParameters(Array.Empty<RTCRtpEncodingParameters>(), null);

            var dto = JsonUtility.FromJson<WebGLRtpSendParametersDto>(json);
            var encodings = Array.Empty<RTCRtpEncodingParameters>();
            if (dto?.encodings != null)
            {
                encodings = new RTCRtpEncodingParameters[dto.encodings.Length];
                for (int i = 0; i < dto.encodings.Length; i++)
                {
                    var src = dto.encodings[i];
                    encodings[i] = new RTCRtpEncodingParameters
                    {
                        active = src.active,
                        maxBitrate = src.maxBitrate == 0 ? (ulong?)null : src.maxBitrate,
                        minBitrate = src.minBitrate == 0 ? (ulong?)null : src.minBitrate,
                        maxFramerate = src.maxFramerate == 0 ? (uint?)null : src.maxFramerate,
                        scaleResolutionDownBy = src.scaleResolutionDownBy <= 0 ? (double?)null : src.scaleResolutionDownBy,
                        rid = string.IsNullOrEmpty(src.rid) ? null : src.rid
                    };
                }
            }

            return new RTCRtpSendParameters(encodings, dto?.transactionId);
        }

        public static string ToSendParametersJson(RTCRtpSendParameters parameters)
        {
            var encodings = parameters.encodings ?? Array.Empty<RTCRtpEncodingParameters>();
            var dto = new WebGLRtpSendParametersDto
            {
                transactionId = parameters.transactionId,
                encodings = new WebGLRtpEncodingDto[encodings.Length]
            };
            for (int i = 0; i < encodings.Length; i++)
            {
                var src = encodings[i];
                dto.encodings[i] = new WebGLRtpEncodingDto
                {
                    active = src.active,
                    maxBitrate = src.maxBitrate.GetValueOrDefault(),
                    minBitrate = src.minBitrate.GetValueOrDefault(),
                    maxFramerate = src.maxFramerate.GetValueOrDefault(),
                    scaleResolutionDownBy = src.scaleResolutionDownBy.GetValueOrDefault(),
                    rid = src.rid
                };
            }

            return JsonUtility.ToJson(dto);
        }

        public static string ToCodecPreferencesJson(RTCRtpCodecCapability[] codecs)
        {
            if (codecs == null || codecs.Length == 0)
                return "[]";

            var parts = new string[codecs.Length];
            for (int i = 0; i < codecs.Length; i++)
            {
                parts[i] = JsonUtility.ToJson(new WebGLRtpCodecDto
                {
                    channels = codecs[i].channels.GetValueOrDefault(),
                    clockRate = codecs[i].clockRate.GetValueOrDefault(),
                    mimeType = codecs[i].mimeType,
                    sdpFmtpLine = codecs[i].sdpFmtpLine
                });
            }

            return "[" + string.Join(",", parts) + "]";
        }

        static RTCRtpCodecCapability ToCodec(WebGLRtpCodecDto dto)
        {
            return new RTCRtpCodecCapability
            {
                channels = dto.channels == 0 ? (int?)null : dto.channels,
                clockRate = dto.clockRate == 0 ? (int?)null : dto.clockRate,
                mimeType = dto.mimeType,
                sdpFmtpLine = string.IsNullOrEmpty(dto.sdpFmtpLine) ? null : dto.sdpFmtpLine
            };
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DelegateWebGLCreateSessionSuccess(IntPtr peerPtr, int sdpType, [MarshalAs(UnmanagedType.LPStr)] string sdp);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DelegateWebGLCreateSessionFailure(IntPtr peerPtr, int errorType, [MarshalAs(UnmanagedType.LPStr)] string message);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DelegateWebGLSetSessionSuccess(IntPtr peerPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DelegateWebGLSetSessionFailure(IntPtr peerPtr, int errorType, [MarshalAs(UnmanagedType.LPStr)] string message);

    [Serializable]
    internal class WebGLTransceiverInitDto
    {
        public int direction;
        public int[] streamPtrs;
    }

    [Serializable]
    internal class WebGLSessionDescriptionDto
    {
        public int type;
        public string sdp;
    }

    [Serializable]
    internal class WebGLRtpCodecDto
    {
        public int channels;
        public int clockRate;
        public string mimeType;
        public string sdpFmtpLine;
    }

    [Serializable]
    internal class WebGLRtpHeaderExtensionDto
    {
        public string uri;
    }

    [Serializable]
    internal class WebGLRtpCapabilitiesDto
    {
        public WebGLRtpCodecDto[] codecs;
        public WebGLRtpHeaderExtensionDto[] headerExtensions;
    }

    [Serializable]
    internal class WebGLRtpEncodingDto
    {
        public bool active = true;
        public ulong maxBitrate;
        public ulong minBitrate;
        public uint maxFramerate;
        public double scaleResolutionDownBy;
        public string rid;
    }

    [Serializable]
    internal class WebGLRtpSendParametersDto
    {
        public WebGLRtpEncodingDto[] encodings;
        public string transactionId;
    }
#endif
}
