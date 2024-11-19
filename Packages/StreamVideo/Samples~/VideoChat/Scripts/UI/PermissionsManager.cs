using System;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
#if UNITY_IOS
using System.Collections;
#endif

namespace StreamVideo.ExampleProject.UI
{
    public class PermissionsManager
    {
        public enum PermissionType
        {
            Camera,
            Microphone
        }

        public PermissionsManager(MonoBehaviour coroutineRunner)
        {
            _coroutineRunner = coroutineRunner;
        }

        public bool HasPermission(PermissionType permissionType)
        {
#if UNITY_ANDROID
            var androidPermission = PermissionTypeToAndroidPermission(permissionType);
            return Permission.HasUserAuthorizedPermission(androidPermission);
#elif UNITY_IOS
            var iosPermission = PermissionTypeToIOSPermission(permissionType);
            return Application.HasUserAuthorization(iosPermission);
#else
            Debug.LogError($"Handling permissions not implemented for platform: " + Application.platform);
#endif
        }


        public void RequestPermission(PermissionType permissionType, Action onGranted = null, Action onDenied = null)
        {
#if UNITY_ANDROID
            RequestAndroidPermission(permissionType, onGranted, onDenied);
#elif UNITY_IOS
            _coroutineRunner.StartCoroutine(RequestIOSPermissionCoroutine(permissionType, onGranted, onDenied));
#else
            Debug.LogError($"Handling permissions not implemented for platform: " + Application.platform);
#endif
        }

        private readonly MonoBehaviour _coroutineRunner;

#if UNITY_ANDROID
        private void RequestAndroidPermission(PermissionType permissionType, Action onGranted = null,
            Action onDenied = null)
        {
            var androidPermission = PermissionTypeToAndroidPermission(permissionType);
            var callbacks = new PermissionCallbacks();
            Permission.RequestUserPermission(androidPermission, callbacks);

            callbacks.PermissionGranted += permissionName =>
            {
                if (androidPermission == permissionName)
                {
                    onGranted?.Invoke();
                }
            };
            callbacks.PermissionDenied += permissionName =>
            {
                if (androidPermission == permissionName)
                {
                    onDenied?.Invoke();
                }
            };
            callbacks.PermissionDeniedAndDontAskAgain += permissionName =>
            {
                if (androidPermission == permissionName)
                {
                    onDenied?.Invoke();
                }
            };
        }

        private string PermissionTypeToAndroidPermission(PermissionType type)
        {
            switch (type)
            {
                case PermissionType.Camera: return Permission.Camera;
                case PermissionType.Microphone: return Permission.Microphone;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
#endif

#if UNITY_IOS
        private IEnumerator RequestIOSPermissionCoroutine(PermissionType permissionType, Action onGranted = null,
            Action onDenied = null)
        {
            var iosPermission = PermissionTypeToIOSPermission(permissionType);
            yield return Application.RequestUserAuthorization(iosPermission);

            if (Application.HasUserAuthorization(iosPermission))
            {
                onGranted?.Invoke();
            }
            else
            {
                onDenied?.Invoke();
            }
        }
        
        UserAuthorization PermissionTypeToIOSPermission(PermissionType type)
        {
            switch (type)
            {
                case PermissionType.Camera: return UserAuthorization.WebCam;
                case PermissionType.Microphone: return UserAuthorization.Microphone;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
#endif
    }
}