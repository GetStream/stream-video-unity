using System;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
#if UNITY_IOS || (UNITY_WEBGL && !UNITY_EDITOR)
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
#if UNITY_STANDALONE
            return true;
#elif UNITY_WEBGL && !UNITY_EDITOR
            // UnityEngine.Microphone is not in the WebGL player. Camera still uses RequestUserAuthorization.
            if (permissionType == PermissionType.Microphone)
                return true;
            var userAuthorization = PermissionTypeToUserAuthorization(permissionType);
            return Application.HasUserAuthorization(userAuthorization);
#elif UNITY_ANDROID
            var androidPermission = PermissionTypeToAndroidPermission(permissionType);
            return Permission.HasUserAuthorizedPermission(androidPermission);
#elif UNITY_IOS
            var userAuthorization = PermissionTypeToUserAuthorization(permissionType);
            return Application.HasUserAuthorization(userAuthorization);
#else
            Debug.LogWarning($"Handling permissions not implemented for platform: {Application.platform}. Requested {permissionType}. Assuming permission is granted.");
            return true;
#endif
        }


        public void RequestPermission(PermissionType permissionType, Action onGranted = null, Action onDenied = null)
        {
#if UNITY_ANDROID
            RequestAndroidPermission(permissionType, onGranted, onDenied);
#elif UNITY_IOS || (UNITY_WEBGL && !UNITY_EDITOR)
            _coroutineRunner.StartCoroutine(RequestUserAuthorizationCoroutine(permissionType, onGranted, onDenied));
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

#if UNITY_IOS || (UNITY_WEBGL && !UNITY_EDITOR)
        private IEnumerator RequestUserAuthorizationCoroutine(PermissionType permissionType, Action onGranted = null,
            Action onDenied = null)
        {
            var userAuthorization = PermissionTypeToUserAuthorization(permissionType);
            yield return Application.RequestUserAuthorization(userAuthorization);

            if (Application.HasUserAuthorization(userAuthorization))
            {
                onGranted?.Invoke();
            }
            else
            {
                onDenied?.Invoke();
            }
        }
        
        UserAuthorization PermissionTypeToUserAuthorization(PermissionType type)
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
