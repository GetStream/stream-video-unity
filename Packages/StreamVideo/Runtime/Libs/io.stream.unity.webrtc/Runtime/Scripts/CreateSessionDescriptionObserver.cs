using System;
using System.Runtime.InteropServices;

namespace Unity.WebRTC
{
    class CreateSessionDescriptionObserver : SafeHandle
    {
        public Action<RTCSdpType, string, RTCErrorType, string> onCreateSessionDescription;

        internal CreateSessionDescriptionObserver()
            : base(IntPtr.Zero, true)
        {
        }

        internal static CreateSessionDescriptionObserver CreateWithHandle(IntPtr handle)
        {
            var observer = new CreateSessionDescriptionObserver();
            observer.SetHandle(handle);
            return observer;
        }

        public void Invoke(RTCSdpType type, string sdp, RTCErrorType errorType, string message)
        {
            onCreateSessionDescription?.Invoke(type, sdp, errorType, message);
        }

        public override bool IsInvalid { get { return handle == IntPtr.Zero; } }

        protected override bool ReleaseHandle()
        {
            onCreateSessionDescription = null;
            return true;
        }
    }
}
