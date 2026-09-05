using BepInEx.Configuration;
using BorkelRNVG.Configuration;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Reflection;
using System;
using System.Linq;
using BorkelRNVG.Globals;
using BorkelRNVG.Models;
using BorkelRNVG.Struct;

namespace BorkelRNVG.Helpers
{
    public static class AssetHelper
    {
        public static readonly string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly string assetsDirectory = $"{directory}\\Assets";

        public static Shader pixelationShader; // Assets/Systems/Effects/Pixelation/Pixelation.shader
        public static Shader nightVisionShader;
        public static Shader contrastShader;
        public static Shader additiveBlendShader;
        public static Shader blurShader;
        public static Shader exposureShader;
        public static Shader maskShader;

        public static Dictionary<string, AudioClip> LoadedAudioClips = [];
        public static Dictionary<string, NvgData> NvgData = [];
        public static Dictionary<string, ThermalData> ThermalData = [];
        public static Dictionary<string, LensLayoutConfig> LensLayouts =
            new Dictionary<string, LensLayoutConfig>(StringComparer.OrdinalIgnoreCase);

        public static void LoadShaders()
        {
            string eftShaderPath = Path.Combine(Environment.CurrentDirectory, "EscapeFromTarkov_Data", "StreamingAssets", "Windows", "shaders");
            string nightVisionShaderPath = ModFiles.BorkelShadersPath;
            string peinShaders = Path.Combine(ModDirectories.ShadersPath, "pein_shaders");

            pixelationShader = FileHelper.LoadShader("Assets/Systems/Effects/Pixelation/Pixelation.shader", eftShaderPath); // T-7 pixelation
            nightVisionShader = FileHelper.LoadShader(
                "Assets/BRNVG_NEW/CustomNightVision.shader", nightVisionShaderPath);
            contrastShader = FileHelper.LoadShader("assets/shaders/pein/shaders/contrastshader.shader", peinShaders);
            additiveBlendShader = FileHelper.LoadShader("assets/shaders/pein/shaders/additiveblendshader.shader", peinShaders);
            blurShader = FileHelper.LoadShader("assets/shaders/pein/shaders/blurshader.shader", peinShaders);
            exposureShader = FileHelper.LoadShader("assets/shaders/pein/shaders/exposureshader.shader", peinShaders);
            maskShader = FileHelper.LoadShader("assets/shaders/pein/shaders/maskshader.shader", peinShaders);
        }

        public static void LoadLensLayouts(ConfigFile config)
        {
            LensLayouts.Clear();
            if (!Directory.Exists(ModDirectories.LensLayoutsPath))
                throw new DirectoryNotFoundException(
                    "Lens layout directory not found: " + ModDirectories.LensLayoutsPath);

            foreach (string filePath in Directory.GetFiles(
                         ModDirectories.LensLayoutsPath, "*.json"))
            {
                LensLayoutDefinition definition = FileHelper.ParseJson<LensLayoutDefinition>(
                    Path.GetDirectoryName(filePath), Path.GetFileName(filePath));
                definition ??= new LensLayoutDefinition();
                if (string.IsNullOrWhiteSpace(definition.Id))
                    definition.Id = Path.GetFileNameWithoutExtension(filePath);
                if (definition.Lenses == null || definition.Lenses.Count == 0)
                    throw new InvalidDataException(
                        "Lens layout '" + definition.Id + "' has no lenses.");
                if (definition.Lenses.Count > 4)
                    throw new InvalidDataException(
                        "Lens layout '" + definition.Id + "' exceeds the four-tube shader limit.");
                if (LensLayouts.ContainsKey(definition.Id))
                    throw new InvalidDataException(
                        "Duplicate lens layout id: " + definition.Id);

                LensLayouts.Add(definition.Id,
                    new LensLayoutConfig(config, definition));
                Plugin.Log("Loaded lens layout " + definition.Id);
            }

            if (LensLayouts.Count == 0)
                throw new InvalidDataException("No lens layouts were loaded.");
        }

        public static LensLayoutDefinition FindLensLayout(string id)
        {
            if (!string.IsNullOrWhiteSpace(id) &&
                LensLayouts.TryGetValue(id, out LensLayoutConfig layout))
                return layout.Values;

            if (LensLayouts.TryGetValue("monocular", out LensLayoutConfig fallback))
                return fallback.Values;

            return LensLayouts.Values.FirstOrDefault()?.Values;
        }

        public static void LoadNvgs(ConfigFile config)
        {
            string[] nvgDirs = Directory.GetDirectories(ModDirectories.NvgPath);
            
            foreach (string nvgDir in nvgDirs)
            {
                NvgItemConfig nvgConfig = FileHelper.ParseJson<NvgItemConfig>(nvgDir, "config.json");
                Texture maskTexture = FileHelper.LoadTexture(Path.Combine(nvgDir, "mask.png"));
                Texture lensTexture = FileHelper.LoadTexture(Path.Combine(nvgDir, "lens.png"));

                if (string.IsNullOrWhiteSpace(nvgConfig.Shader.LensLayout) ||
                    !LensLayouts.TryGetValue(
                        nvgConfig.Shader.LensLayout,
                        out LensLayoutConfig referencedLayout))
                    throw new InvalidDataException(
                        "NVG '" + nvgConfig.Category + "' references unknown lens layout '" +
                        nvgConfig.Shader.LensLayout + "'.");
                nvgConfig.Shader.LensLayout = referencedLayout.Values.Id;
                NightVisionConfig nightVisionConfig = new NightVisionConfig(
                    config, nvgConfig.Category + " - NVG Settings",
                    nvgConfig.Shader, LensLayouts.Keys);
                
                if (nvgConfig.ItemId != null)
                {
                    NvgData nvgData = new NvgData()
                    {
                        NvgItemConfig = nvgConfig,
                        MaskTexture = maskTexture,
                        LensTexture = lensTexture,
                        NightVisionConfig = nightVisionConfig
                    };
                    
                    NvgData.Add(nvgConfig.ItemId, nvgData);
                    
                    Plugin.Log($"Loaded Nvg {nvgConfig.Category} with id: {nvgConfig.ItemId}");
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
                            NightVisionConfig = nightVisionConfig
                        };
                        
                        NvgData.Add(itemId, nvgData);
                    }
                }
            }
        }
        
        public static void LoadThermals(ConfigFile config)
        {
            string[] thermalDirs = Directory.GetDirectories(ModDirectories.ThermalPath);
            
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
                    VerticalResolution = thermalConfig.VerticalResolution,
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
                    
                    Plugin.Log($"Loaded thermal {thermalConfig.Category} with id: {thermalConfig.ItemId}");
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
                Plugin.Log(ex.ToString());
            } 
        }
    }
}
