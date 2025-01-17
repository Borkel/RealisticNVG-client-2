﻿using BepInEx;
using BepInEx.Configuration;
using BorkelRNVG.Patches;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using WindowsInput.Native;
using Comfort.Common;

namespace BorkelRNVG
{
    [BepInPlugin("com.borkel.nvgmasks", "Borkel's Realistic NVGs", "1.6.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static readonly string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly string assetsDirectory = $"{directory}\\Assets";

        //mask textures
        public static Texture2D maskAnvis;
        public static Texture2D maskBino;
        public static Texture2D maskMono;
        public static Texture2D maskPnv;
        public static Texture2D maskThermal;
        public static Texture2D maskPixel; //i don't really know if this one does anything

        //noise texture
        public static Texture2D Noise;
        //public static Texture2D maskFlare;
        public static Shader pixelationShader; //Assets/Systems/Effects/Pixelation/Pixelation.shader
        public static Shader nightVisionShader; // Assets/Shaders/CustomNightVision.shader

        //lens textures
        public static Texture2D lensAnvis;
        public static Texture2D lensBino;
        public static Texture2D lensMono;
        public static Texture2D lensPnv;

        //global config stuff
        public static string miscCategory = "0. Miscellaneous";
        public static string gatingCategory = "1. Gating";
        public static string globalCategory = "2. Global";
        public static ConfigEntry<float> globalMaskSize;
        public static ConfigEntry<float> globalGain;

        //gpnvg18 config stuff
        public static string gpnvgCategory = "3. GPNVG-18";
        public static ConfigEntry<float> quadR;
        public static ConfigEntry<float> quadG;
        public static ConfigEntry<float> quadB;
        public static ConfigEntry<float> quadMaskSize;
        public static ConfigEntry<float> quadNoiseIntensity;
        public static ConfigEntry<float> quadNoiseSize;
        public static ConfigEntry<float> quadGain;

        //pvs14 config stuff
        public static string pvsCategory = "4. PVS-14";
        public static ConfigEntry<float> pvsR;
        public static ConfigEntry<float> pvsG;
        public static ConfigEntry<float> pvsB;
        public static ConfigEntry<float> pvsMaskSize;
        public static ConfigEntry<float> pvsNoiseIntensity;
        public static ConfigEntry<float> pvsNoiseSize;
        public static ConfigEntry<float> pvsGain;

        //n15 config stuff
        public static string nCategory = "5. N-15";
        public static ConfigEntry<float> nR;
        public static ConfigEntry<float> nG;
        public static ConfigEntry<float> nB;
        public static ConfigEntry<float> nMaskSize;
        public static ConfigEntry<float> nNoiseIntensity;
        public static ConfigEntry<float> nNoiseSize;
        public static ConfigEntry<float> nGain;

        //pnv10t config stuff
        public static string pnvCategory = "6. PNV-10T";
        public static ConfigEntry<float> pnvR;
        public static ConfigEntry<float> pnvG;
        public static ConfigEntry<float> pnvB;
        public static ConfigEntry<float> pnvMaskSize;
        public static ConfigEntry<float> pnvNoiseIntensity;
        public static ConfigEntry<float> pnvNoiseSize;
        public static ConfigEntry<float> pnvGain;

        //t-7 config
        public static string t7Category = "7. T-7";
        public static ConfigEntry<bool> t7Pixelation;
        public static ConfigEntry<bool> t7HzLock;

        //sprint patch stuff
        public static ConfigEntry<bool> enableSprintPatch;
        public static bool isSprinting = false;
        public static bool wasSprinting = false;
        public static Dictionary<string, bool> LightDictionary = new Dictionary<string, bool>();

        //UltimateBloom stuff
        //public static BloomAndFlares BloomAndFlaresInstance;
        //public static UltimateBloom UltimateBloomInstance;

        //Reshade stuff
        public static VirtualKeyCode nvgKey = VirtualKeyCode.NUMPAD0;
        public static ConfigEntry<bool> enableReshade;
        public static ConfigEntry<bool> disableReshadeInMenus;
        //public static bool disabledInMenu = false;

        //Gating
        public static ConfigEntry<KeyCode> gatingInc;
        public static ConfigEntry<KeyCode> gatingDec;
        public static ConfigEntry<int> gatingLevel;
        public static bool nvgOn = false;

        //Audio
        public static Dictionary<string, AudioClip> LoadedAudioClips = new Dictionary<string, AudioClip>();
        public static bool HasReloadedAudio = false;

        private static readonly Dictionary<Texture, Texture> maskToLens = new Dictionary<Texture, Texture>();

        private void Awake()
        {
            // BepInEx F12 menu
            
            // Miscellaneous
            enableSprintPatch = Config.Bind(miscCategory, "Sprint toggles tactical devices. DO NOT USE WITH FIKA.", false, "Sprinting will toggle tactical devices until you stop sprinting, this mitigates the IR lights being visible outside of the NVGs. I recommend enabling this feature.");
            enableReshade = Config.Bind(miscCategory, "Enable ReShade input simulation", false, "Will enable the input simulation to enable the ReShade, will use numpad keys. GPNVG-18 -> numpad 9. PVS-14 -> numpad 8. N-15 -> numpad 7. PNV-10T -> numpad 6. Off -> numpad 5. Only enable if you've installed the ReShade.");
            disableReshadeInMenus = Config.Bind(miscCategory, "Disable ReShade when in menus", true, "Is a bit wonky in the hideout, but works well in-raid.");
            
            // Gating
            gatingInc = Config.Bind(gatingCategory, "1. Manual gating increase", KeyCode.None, "Increases the gain by 1 step. There's 5 levels (-2...2), default level is the third level (0).");
            gatingDec = Config.Bind(gatingCategory, "2. Manual gating decrease", KeyCode.None, "Decreases the gain by 1 step. There's 5 levels (-2...2), default level is the third level (0).");
            gatingLevel = Config.Bind(gatingCategory, "Gating level", 0, "Will reset when the game opens. You are supposed to use the gating increase/decrease keys to change the gating level, but you are free to change it manually if you want to make sure you are at a specific gating level.");
            
            // Global multipliers
            globalMaskSize = Config.Bind(globalCategory, "Mask size multiplier", 1.07f, new ConfigDescription("Applies size multiplier to all masks", new AcceptableValueRange<float>(0f, 2f)));
            globalGain = Config.Bind(globalCategory, "Gain multiplier", 1f, new ConfigDescription("Applies gain multiplier to all NVGs", new AcceptableValueRange<float>(0f, 5f)));
            
            // GPNVG-18 config. Mask size should be 0.96 times of the rest
            quadGain = Config.Bind(gpnvgCategory, "1. Gain", 2.5f, new ConfigDescription("Light amplification", new AcceptableValueRange<float>(0f, 5f)));
            quadNoiseIntensity = Config.Bind(gpnvgCategory, "2. Noise intensity", 0.2f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 1f)));
            quadNoiseSize = Config.Bind(gpnvgCategory, "3. Noise scale", 0.1f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 0.99f)));
            quadMaskSize = Config.Bind(gpnvgCategory, "4. Mask size", 0.96f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 2f)));
            quadR = Config.Bind(gpnvgCategory, "5. Red", 152f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 255f)));
            quadG = Config.Bind(gpnvgCategory, "6. Green", 214f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 255f)));
            quadB = Config.Bind(gpnvgCategory, "7. Blue", 252f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 255f)));
            
            //PVS-14 config
            pvsGain = Config.Bind(pvsCategory, "1. Gain", 2.4f, new ConfigDescription("Light amplification", new AcceptableValueRange<float>(0f, 5f)));
            pvsNoiseIntensity = Config.Bind(pvsCategory, "2. Noise intensity", 0.2f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 1f)));
            pvsNoiseSize = Config.Bind(pvsCategory, "3. Noise scale", 0.1f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 0.99f)));
            pvsMaskSize = Config.Bind(pvsCategory, "4. Mask size", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 2f)));
            pvsR = Config.Bind(pvsCategory, "5. Red", 95f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 255f)));
            pvsG = Config.Bind(pvsCategory, "6. Green", 210f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 255f)));
            pvsB = Config.Bind(pvsCategory, "7. Blue", 255f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 255f)));
            
            //N-15 config
            nGain = Config.Bind(nCategory, "1. Gain", 2.1f, new ConfigDescription("Light amplification", new AcceptableValueRange<float>(0f, 5f)));
            nNoiseIntensity = Config.Bind(nCategory, "2. Noise intensity", 0.25f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 1f)));
            nNoiseSize = Config.Bind(nCategory, "3. Noise scale", 0.15f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 0.99f)));
            nMaskSize = Config.Bind(nCategory, "4. Mask size", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 2f)));
            nR = Config.Bind(nCategory, "5. Red", 60f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 255f)));
            nG = Config.Bind(nCategory, "6. Green", 235f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 255f)));
            nB = Config.Bind(nCategory, "7. Blue", 100f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 255f)));
            
            //PNV-10T config
            pnvGain = Config.Bind(pnvCategory, "1. Gain", 1.8f, new ConfigDescription("Light amplification", new AcceptableValueRange<float>(0f, 5f)));
            pnvNoiseIntensity = Config.Bind(pnvCategory, "2. Noise intensity", 0.3f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 1f)));
            pnvNoiseSize = Config.Bind(pnvCategory, "3. Noise scale", 0.2f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 0.99f)));
            pnvMaskSize = Config.Bind(pnvCategory, "4. Mask size", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.01f, 2f)));
            pnvR = Config.Bind(pnvCategory, "5. Red", 60f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 255f)));
            pnvG = Config.Bind(pnvCategory, "6. Green", 210f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 255f)));
            pnvB = Config.Bind(pnvCategory, "7. Blue", 60f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 255f)));
            
            //T-7 config
            t7Pixelation = Config.Bind(t7Category, "1. Pixelation", true, "Requires restart. Pixelates the T-7, like a real digital screen");
            t7HzLock = Config.Bind(t7Category, "1. Hz lock", true, "Requires restart. Locks the Hz of the T-7 to 60Hz, like a real digital screen");

            // other variables.. idk
            gatingLevel.Value = 0;            
            string eftShaderPath = Path.Combine(Environment.CurrentDirectory, "EscapeFromTarkov_Data", "StreamingAssets", "Windows", "shaders");

            // loading from PNGs, like Fontaine suggested
            string anvisPath = $"{assetsDirectory}\\MaskTextures\\mask_anvis.png";
            string binoPath = $"{assetsDirectory}\\MaskTextures\\mask_binocular.png";
            string monoPath = $"{assetsDirectory}\\MaskTextures\\mask_old_monocular.png";
            string pnvPath = $"{assetsDirectory}\\MaskTextures\\mask_pnv.png";
            string thermalPath = $"{assetsDirectory}\\MaskTextures\\mask_thermal.png";
            string pixelPath = $"{assetsDirectory}\\MaskTextures\\pixel_mask1.png";
            string noisePath = $"{assetsDirectory}\\MaskTextures\\Noise.png";
            string lensAnvisPath = $"{assetsDirectory}\\LensTextures\\lens_anvis.png";
            string lensBinoPath = $"{assetsDirectory}\\LensTextures\\lens_binocular.png";
            string lensMonoPath = $"{assetsDirectory}\\LensTextures\\lens_old_monocular.png";
            string lensPnvPath = $"{assetsDirectory}\\LensTextures\\lens_pnv.png";
            try
            {
                loadAudioClips();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception);
            }

            //string flarePath = $"{pluginDirectory}\\MaskTextures\\FlareMask.png";
            maskAnvis = LoadPNG(anvisPath);
            maskBino = LoadPNG(binoPath);
            maskMono = LoadPNG(monoPath);
            maskPnv = LoadPNG(pnvPath);
            maskThermal = LoadPNG(thermalPath);
            maskPixel = LoadPNG(pixelPath);//might not do anything really
            lensAnvis = LoadPNG(lensAnvisPath);
            lensBino = LoadPNG(lensBinoPath);
            lensMono = LoadPNG(lensMonoPath);
            lensPnv = LoadPNG(lensPnvPath);
            Noise = LoadPNG(noisePath);
            Noise.wrapMode = TextureWrapMode.Repeat;

            //maskFlare= LoadPNG(flarePath);
            if (maskAnvis == null || maskBino == null || maskMono == null || maskPnv == null || maskThermal == null || maskPixel == null
                || lensAnvis == null || lensBino == null || lensMono == null || lensPnv == null || Noise == null)
            {
                Logger.LogError($"Error loading PNGs. Patches will be disabled.");
                return;
            }

            maskToLens.Add(maskAnvis, lensAnvis);
            maskToLens.Add(maskBino, lensBino);
            maskToLens.Add(maskMono, lensMono);
            maskToLens.Add(maskPnv, lensPnv);

            string nightVisionShaderPath = $"{assetsDirectory}\\Shaders\\borkel_realisticnvg_shaders";
            pixelationShader = LoadShader("Assets/Systems/Effects/Pixelation/Pixelation.shader", eftShaderPath); //to pixelate the T-7
            nightVisionShader = LoadShader("Assets/Shaders/CustomNightVision.shader", nightVisionShaderPath);
            if (pixelationShader == null || nightVisionShader == null)
            {
                Logger.LogError($"Error loading shaders. Patches will be disabled.");
                return;
            }

            // create nvg config classes
            NightVisionItemConfig.InitializeNVGs();

            try
            {
                new NightVisionAwakePatch().Enable();
                new NightVisionApplySettingsPatch().Enable();
                new NightVisionSetMaskPatch().Enable();
                new ThermalVisionSetMaskPatch().Enable();
                new SprintPatch().Enable();
                new NightVisionMethod_1().Enable(); //reshade
                new MenuPatch().Enable(); //reshade
                Logger.LogInfo("Patches enabled successfully!");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception);
            }
            
            // umm......
            //new VignettePatch().Enable();
            //new EndOfRaid().Enable(); //reshade
            //new WeaponSwapPatch().Enable(); //not working
            //new UltimateBloomPatch().Enable(); //works if Awake is prevented from running
            //new LevelSettingsPatch().Enable();

            var controller = new GameObject("BorkelRNVG").AddComponent<BorkelRNVGController>();
            DontDestroyOnLoad(controller.gameObject);
        }

        void Update()
        {
            if(nvgOn)
            {
                if(Input.GetKeyDown(gatingInc.Value) && gatingLevel.Value < 2)
                {
                    gatingLevel.Value++;
                    Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), LoadedAudioClips["gatingKnob.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 1.0f, EOcclusionTest.None, null, false);
                }
                else if(Input.GetKeyUp(gatingDec.Value) && gatingLevel.Value > -2)
                {
                    gatingLevel.Value--;
                    Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), LoadedAudioClips["gatingKnob.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100, 1.0f, EOcclusionTest.None, null, false);
                }
            }
        }

        public static Texture GetMatchingLensMask(Texture mask)
        {
            maskToLens.TryGetValue(mask, out var lens);
            return lens;
        }

        private static Texture2D LoadPNG(string filePath)
        {
            Texture2D tex = null;
            byte[] fileData;

            if (File.Exists(filePath))
            {
                fileData = File.ReadAllBytes(filePath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
                tex.wrapMode = TextureWrapMode.Clamp; //otherwise the mask will repeat itself around screen borders
            }

            return tex;
        }

        private static Shader LoadShader(string shaderName, string bundlePath)
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(bundlePath);
            Shader sh = assetBundle.LoadAsset<Shader>(shaderName);
            assetBundle.Unload(false);
            return sh;
        }

        private void loadAudioClips()
        {
            string[] audioFilesDir = Directory.GetFiles($"{assetsDirectory}\\Sounds");
            LoadedAudioClips.Clear();

            foreach (string fileDir in audioFilesDir)
            {
                loadAudioClip(fileDir);
            }

            HasReloadedAudio = true;
        }

        private async void loadAudioClip(string path)
        {
            LoadedAudioClips[Path.GetFileName(path)] = await requestAudioClip(path);
        }

        private async Task<AudioClip> requestAudioClip(string path)
        {
            string extension = Path.GetExtension(path);
            AudioType audioType = AudioType.WAV;

            switch (extension)
            {
                case ".wav":
                    audioType = AudioType.WAV;
                    break;
                case ".ogg":
                    audioType = AudioType.OGGVORBIS;
                    break;
            }

            UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, audioType);
            UnityWebRequestAsyncOperation sendWeb = uwr.SendWebRequest();

            while (!sendWeb.isDone)
                await Task.Yield();

            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Logger.LogError("BRNVG Mod: Failed To Fetch Audio Clip");
                return null;
            }
            else
            {
                AudioClip audioclip = DownloadHandlerAudioClip.GetContent(uwr);
                return audioclip;
            }
        }
    }
}
