using System;
using System.Runtime.InteropServices;

namespace Unity.WebRTC
{
    class SetSessionDescriptionObserver : SafeHandle
    {
        public Action<RTCErrorType, string> onSetSessionDescription;

        internal SetSessionDescriptionObserver()
            : base(IntPtr.Zero, true)
        {
        }

        internal static SetSessionDescriptionObserver CreateWithHandle(IntPtr handle)
        {
            var observer = new SetSessionDescriptionObserver();
            observer.SetHandle(handle);
            return observer;
        }

        public void Invoke(RTCErrorType type, string message)
        {
            onSetSessionDescription?.Invoke(type, message);
        }

        public override bool IsInvalid { get { return handle == IntPtr.Zero; } }

        protected override bool ReleaseHandle()
        {
            onSetSessionDescription = null;
            return true;
        }
    }
}
