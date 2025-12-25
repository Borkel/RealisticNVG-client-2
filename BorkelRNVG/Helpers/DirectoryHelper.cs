using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace BorkelRNVG.Helpers
{
    public static class DirectoryHelper
    {
        public static List<T> ParseDirectory<T>(string path)
        {
            string[] files = Directory.GetFiles(path);
            List<T> parsedFiles = [];

            foreach (string file in files)
            {
                if (file.EndsWith(".json") || file.EndsWith(".jsonc"))
                {
                    parsedFiles.Add(FileHelper.ParseJson<T>(path, file));
                }
            }

            return parsedFiles;
        }

        public static async Task<List<AudioClip>> LoadAudioClipsFromDirectory(string path)
        {
            List<AudioClip> audioClips = [];
            string[] files = Directory.GetFiles(path);

            foreach (string file in files)
            {
                AudioClip clip = await FileHelper.LoadAudioClip(path, file);
                audioClips.Add(clip);
            }

            return audioClips;
        }
    }
}
