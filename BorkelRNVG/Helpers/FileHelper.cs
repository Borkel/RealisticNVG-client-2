using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace BorkelRNVG.Helpers
{
    public static class FileHelper
    {
        public static T ParseJson<T>(string path, string fileName)
        {
            string filePath = Path.Combine(path, fileName);
            
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
        }

        public static async Task<AudioClip> LoadAudioClip(string path, string fileName)
        {
            string finalPath = Path.Combine(path, fileName);

            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip($"file://{finalPath}", AudioType.WAV);
            UnityWebRequestAsyncOperation request = www.SendWebRequest();
            
            while (!request.isDone) await Task.Yield();

            if (www.isHttpError || www.isNetworkError) return null;
                
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            clip.name = Path.GetFileName(fileName);

            return clip;
        }
        
        public static Texture LoadTexture(string filePath, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            tex.LoadImage(File.ReadAllBytes(filePath));
            tex.wrapMode = wrapMode;
            
            return tex;
        }

        public static Shader LoadShader(string shaderName, string bundlePath)
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(bundlePath);
            Shader shader = assetBundle.LoadAsset<Shader>(shaderName);
            assetBundle.Unload(false);
            return shader;
        }

        public static ComputeShader LoadComputeShader(string shaderName, string bundlePath)
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(bundlePath);
            ComputeShader computeShader = assetBundle.LoadAsset<ComputeShader>(shaderName);
            assetBundle.Unload(false);
            return computeShader;
        }
    }
}
