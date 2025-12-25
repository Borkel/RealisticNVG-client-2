using BepInEx.Configuration;
using BorkelRNVG.Configuration;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Reflection;
using System;
using BorkelRNVG.Globals;
using BorkelRNVG.Models;
using BorkelRNVG.Struct;
using EFT;
using System.Threading.Tasks;

namespace BorkelRNVG.Helpers
{
    public static class AssetHelper
    {
        public static readonly string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly string assetsDirectory = $"{directory}\\Assets";

        public static Shader pixelationShader; // Assets/Systems/Effects/Pixelation/Pixelation.shader
        public static Shader nightVisionShader; // Assets/Shaders/CustomNightVision.shader
        public static Shader contrastShader;
        public static Shader additiveBlendShader;
        public static Shader blurShader;
        public static Shader exposureShader;
        public static Shader maskShader;

        public static Texture noiseTexture;
        public static Texture pixelTexture;

        public static Dictionary<string, AudioClip> LoadedAudioClips = [];
        public static Dictionary<string, NvgData> NvgData = [];
        public static Dictionary<string, ThermalData> ThermalData = [];

        public static void LoadShaders()
        {
            string eftShaderPath = Path.Combine(Environment.CurrentDirectory, "EscapeFromTarkov_Data", "StreamingAssets", "Windows", "shaders");
            string nightVisionShaderPath = $"{assetsDirectory}\\Shaders\\borkel_realisticnvg_shaders";
            string peinShaders = Path.Combine(ModDirectories.ShadersPath, "pein_shaders");

            pixelationShader = FileHelper.LoadShader("Assets/Systems/Effects/Pixelation/Pixelation.shader", eftShaderPath); // T-7 pixelation
            nightVisionShader = FileHelper.LoadShader("Assets/Shaders/CustomNightVision.shader", nightVisionShaderPath);
            contrastShader = FileHelper.LoadShader("assets/shaders/pein/shaders/contrastshader.shader", peinShaders);
            additiveBlendShader = FileHelper.LoadShader("assets/shaders/pein/shaders/additiveblendshader.shader", peinShaders);
            blurShader = FileHelper.LoadShader("assets/shaders/pein/shaders/blurshader.shader", peinShaders);
            exposureShader = FileHelper.LoadShader("assets/shaders/pein/shaders/exposureshader.shader", peinShaders);
            maskShader = FileHelper.LoadShader("assets/shaders/pein/shaders/maskshader.shader", peinShaders);
        }

        public static void LoadNvgs(ConfigFile config)
        {
            string[] nvgDirs = Directory.GetDirectories(ModDirectories.NvgPath);
            
            noiseTexture = FileHelper.LoadTexture(Path.Combine(ModDirectories.CommonAssetsPath, "noise.png"), TextureWrapMode.Repeat);
            
            foreach (string nvgDir in nvgDirs)
            {
                NvgItemConfig nvgConfig = FileHelper.ParseJson<NvgItemConfig>(nvgDir, "config.json");
                Texture maskTexture = FileHelper.LoadTexture(Path.Combine(nvgDir, "mask.png"));
                Texture lensTexture = FileHelper.LoadTexture(Path.Combine(nvgDir, "lens.png"));

                NightVisionConfigStruct configStruct = new NightVisionConfigStruct()
                {
                    Gain = nvgConfig.Gain,
                    NoiseIntensity = nvgConfig.NoiseIntensity,
                    NoiseSize = nvgConfig.NoiseSize,
                    MaskSize = nvgConfig.MaskSize,
                    Red = nvgConfig.Red,
                    Green = nvgConfig.Green,
                    Blue = nvgConfig.Blue,
                    GatingType = nvgConfig.GatingType,
                    GatingSpeed = nvgConfig.GatingSpeed,
                    MaxBrightness = nvgConfig.MaxBrightness,
                    MinBrightness = nvgConfig.MinBrightness,
                    MaxBrightnessThreshold = nvgConfig.MaxBrightnessThreshold,
                    MinBrightnessThreshold = nvgConfig.MinBrightnessThreshold
                };
                
                if (nvgConfig.ItemId != null)
                {
                    NvgData nvgData = new NvgData()
                    {
                        NvgItemConfig = nvgConfig,
                        MaskTexture = maskTexture,
                        LensTexture = lensTexture,
                        NightVisionConfig = new NightVisionConfig(config, nvgConfig.Category, configStruct)
                    };
                    
                    NvgData.Add(nvgConfig.ItemId, nvgData);
                    
                    Plugin.Logger.LogInfo($"Loaded Nvg {nvgConfig.Category} with id: {nvgConfig.ItemId}");
                    continue;
                }

                if (nvgConfig.ItemIds.Count > 0)
                {
                    foreach (string itemId in nvgConfig.ItemIds)
                    {
                        NvgData nvgData = new NvgData()
                        {
                            NvgItemConfig = nvgConfig,
                            MaskTexture = maskTexture,
                            LensTexture = lensTexture,
                            NightVisionConfig = new NightVisionConfig(config, nvgConfig.Category, configStruct)
                        };
                        
                        NvgData.Add(itemId, nvgData);
                    }
                }
            }
        }
        
        public static void LoadThermals(ConfigFile config)
        {
            string[] thermalDirs = Directory.GetDirectories(ModDirectories.ThermalPath);
            
            pixelTexture = FileHelper.LoadTexture(Path.Combine(ModDirectories.CommonAssetsPath, "pixel_mask.png"), TextureWrapMode.Repeat);
            
            foreach (string thermalDir in thermalDirs)
            {
                ThermalItemConfig thermalConfig = FileHelper.ParseJson<ThermalItemConfig>(thermalDir, "config.json");
                Texture maskTexture = FileHelper.LoadTexture(Path.Combine(thermalDir, "mask.png"));
                Texture lensTexture = FileHelper.LoadTexture(Path.Combine(thermalDir, "lens.png"));

                ThermalConfigStruct configStruct = new ThermalConfigStruct()
                {
                    IsFpsStuck = thermalConfig.IsFpsStuck,
                    MinFps = thermalConfig.MinFps,
                    MaxFps = thermalConfig.MaxFps,
                    IsMotionBlurred = thermalConfig.IsMotionBlurred,
                    IsNoisy = thermalConfig.IsNoisy,
                    IsPixelated = thermalConfig.IsPixelated,
                };
                
                if (thermalConfig.ItemId != null)
                {
                    ThermalData thermalData = new ThermalData()
                    {
                        ThermalItemConfig = thermalConfig,
                        MaskTexture = maskTexture,
                        LensTexture = lensTexture,
                        ThermalConfig = new ThermalConfig(config, thermalConfig.Category, configStruct)
                    };
                    
                    ThermalData.Add(thermalConfig.ItemId, thermalData);
                    
                    Plugin.Logger.LogInfo($"Loaded thermal {thermalConfig.Category} with id: {thermalConfig.ItemId}");
                    continue;
                }

                if (thermalConfig.ItemIds.Count > 0)
                {
                    foreach (string itemId in thermalConfig.ItemIds)
                    {
                        ThermalData thermalData = new ThermalData()
                        {
                            ThermalItemConfig = thermalConfig,
                            MaskTexture = maskTexture,
                            LensTexture = lensTexture,
                            ThermalConfig = new ThermalConfig(config, thermalConfig.Category, configStruct)
                        };
                        
                        ThermalData.Add(itemId, thermalData);
                    }
                }
            }
        }

        public static async void LoadAudioClips()
        {
            try
            {
                List<AudioClip> audioClips = await DirectoryHelper.LoadAudioClipsFromDirectory(ModDirectories.SoundsPath);

                foreach (AudioClip audioClip in audioClips)
                {
                    LoadedAudioClips[audioClip.name] = audioClip;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError(ex);
            } 
        }
    }
}
