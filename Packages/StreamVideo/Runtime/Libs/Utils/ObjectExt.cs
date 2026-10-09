using UnityEngine;
using Object = UnityEngine.Object;

namespace StreamVideo.Libs.Utils
{
    /// <summary>
    /// <see cref="Object.Destroy"/> while playing, <see cref="Object.DestroyImmediate"/> in edit mode.
    /// </summary>
    public static class ObjectExt
    {
        public static void SmartDestroy(this Object obj)
        {
            if (obj == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Object.Destroy(obj);
            }
            else
            {
                Object.DestroyImmediate(obj);
            }
#else
            Object.Destroy(obj);
#endif
        }
    }
}
