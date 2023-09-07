using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("Unity.WebRTC")]
[assembly: InternalsVisibleTo("Stream.ExampleProject")]
namespace StreamVideo.Core.Utils
{
    //StreamTodo: remove
    internal static class TextureUtils
    {
        public static byte[] ComputeTextureHash(WebCamTexture texture)
        {
            Texture2D tex2D = new Texture2D(texture.width, texture.height);
            tex2D.SetPixels(texture.GetPixels());
            tex2D.Apply();

            if (tex2D != null)
            {
                byte[] textureBytes = tex2D.GetRawTextureData();
                using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                {
                    return md5.ComputeHash(textureBytes);
                }
            }
            
            Debug.LogError("Failed to compute texture hash");
            return null;
        }

        public static bool ByteArraysEqual(byte[] b1, byte[] b2)
        {
            if (b1.Length != b2.Length)
                return false;

            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i])
                    return false;
            }

            return true;
        }
    }
}