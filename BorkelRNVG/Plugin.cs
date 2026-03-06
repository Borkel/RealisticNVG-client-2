using BepInEx;
using BepInEx.Configuration;
using BorkelRNVG.Patches;
using System;
using System.Collections.Generic;
using UnityEngine;
using Comfort.Common;
using BepInEx.Logging;
using BorkelRNVG.Helpers;
using HarmonyLib;

namespace BorkelRNVG
{
    [BepInPlugin("com.borkel.nvgmasks", "Borkel's Realistic NVGs", "2.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static new ManualLogSource Logger;
        public static Harmony harmony = new Harmony("com.borkel.nvgmasks");
        
        public static ConfigEntry<bool> debugLogging;

        // global
        public static ConfigEntry<float> globalMaskSize;
        public static ConfigEntry<float> globalGain;
        public static ConfigEntry<bool> allowAmbientChange;
        public static ConfigEntry<bool> globalLensDistortion;
        public static ConfigEntry<bool> globalNearBlur;
        public static ConfigEntry<float> globalBlurIntensity;
        public static ConfigEntry<float> globalBlurDistance;
        public static ConfigEntry<int> globalBlurQuality;

        //sprint patch stuff
        public static ConfigEntry<bool> enableSprintPatch;
        public static bool isSprinting = false;
        public static bool wasSprinting = false;
        public static Dictionary<string, bool> LightDictionary = new Dictionary<string, bool>();

        //UltimateBloom stuff
        //public static BloomAndFlares BloomAndFlaresInstance;
        //public static UltimateBloom UltimateBloomInstance;

        // IR illumination
        public static ConfigEntry<float> irFlashlightBrightnessMult;
        public static ConfigEntry<float> irFlashlightRangeMult;
        public static ConfigEntry<float> irLaserBrightnessMult;
        public static ConfigEntry<float> irLaserRangeMult;
        public static ConfigEntry<float> irLaserPointClose;
        public static ConfigEntry<float> irLaserPointFar;
        //public static bool disabledInMenu = false;

        // Gating
        public static ConfigEntry<KeyCode> gatingInc;
        public static ConfigEntry<KeyCode> gatingDec;
        public static ConfigEntry<int> gatingLevel;
        public static ConfigEntry<bool> enableAutoGating;
        public static ConfigEntry<bool> clampMinGating;
        public static ConfigEntry<bool> gatingDebug;

        private void Awake()
        {
            // BepInEx F12 menu
            Logger = base.Logger;

            // Miscellaneous
            enableSprintPatch = Config.Bind(Category.miscCategory, "Sprint toggles tactical devices. DO NOT USE WITH FIKA.", false, "Sprinting will toggle tactical devices until you stop sprinting, this mitigates the IR lights being visible outside of the NVGs. I recommend enabling this feature.");
            debugLogging = Config.Bind(Category.miscCategory, "Enable Debug Logging", false, "Enables debug logging.");
            
            // Global
            globalMaskSize = Config.Bind(Category.globalCategory, "1. Mask size multiplier", 1.07f, new ConfigDescription("Applies size multiplier to all masks", new AcceptableValueRange<float>(0f, 2f)));
            globalMaskSize.SettingChanged += (_, _) => NvgHelper.ApplyNightVisionSettings();
            globalGain = Config.Bind(Category.globalCategory, "2. Gain multiplier", 1f, new ConfigDescription("Applies gain multiplier to all NVGs", new AcceptableValueRange<float>(0f, 5f)));
            globalGain.SettingChanged += (_, _) => NvgHelper.ApplyNightVisionSettings();
            allowAmbientChange = Config.Bind(Category.globalCategory, "3. Allow ambient change", true, new ConfigDescription("Toggles whether night vision affects ambient lighting.", null));
            allowAmbientChange.SettingChanged += (sender, e) => AmbientPatch.TogglePatch(!allowAmbientChange.Value);
            
            // Global, lens distortion settings
            globalLensDistortion = Config.Bind(Category.globalCategory, "4. Enable lens distortion", true, new ConfigDescription("Toggles lens distortion for all NVGs.", null));
            globalLensDistortion.SettingChanged += (_, _) => NvgHelper.ApplyNightVisionSettings();
            globalNearBlur = Config.Bind(Category.globalCategory, "5. Enable near blur", true, new ConfigDescription("Toggles near blur for all NVGs.", null));
            globalNearBlur.SettingChanged += (_, _) => NvgHelper.ApplyNightVisionSettings();
            globalBlurIntensity = Config.Bind(Category.globalCategory, "6. Near blur intensity", 20f, new ConfigDescription("Increases intensity of blur.", new AcceptableValueRange<float>(0f, 100f)));
            globalBlurIntensity.SettingChanged += (_, _) => NvgHelper.ApplyNightVisionSettings();
            globalBlurDistance = Config.Bind(Category.globalCategory, "7. Near blur distance", 4f, new ConfigDescription("Distance at which the blur disappears.", new AcceptableValueRange<float>(0f, 20f)));
            globalBlurDistance.SettingChanged += (_, _) => NvgHelper.ApplyNightVisionSettings();
            globalBlurQuality = Config.Bind(Category.globalCategory, "8. Near blur quality", 4, new ConfigDescription("Changes the size of the gauss kernel, affecting quality.", new AcceptableValueRange<int>(1, 12)));
            globalBlurQuality.SettingChanged += (_, _) => NvgHelper.ApplyNightVisionSettings();

            // Gating
            gatingInc = Config.Bind(Category.gatingCategory, "1. Manual gating increase", KeyCode.None, "Increases the gain by 1 step. There's 5 levels (-2...2), default level is the third level (0).");
            gatingDec = Config.Bind(Category.gatingCategory, "2. Manual gating decrease", KeyCode.None, "Decreases the gain by 1 step. There's 5 levels (-2...2), default level is the third level (0).");
            gatingLevel = Config.Bind(Category.gatingCategory, "3. Gating level", 0, "Will reset when the game opens. You are supposed to use the gating increase/decrease keys to change the gating level, but you are free to change it manually if you want to make sure you are at a specific gating level.");
            enableAutoGating = Config.Bind(Category.gatingCategory, "4. Enable Auto-Gating", false, "EXPERIMENTAL! WILL REDUCE FPS! Enables auto-gating (automatic brightness adjustment) for certain night vision devices. Auto-gating WILL NOT work without this enabled.");
            clampMinGating = Config.Bind(Category.gatingCategory, "5. Clamp Minimum Gating Multiplier", true, "Clamps the minimum brightness multiplier to the night vision device's minimum brightness multiplier. If disabled, night vision can become fully dark during automatic fire.");
            gatingDebug = Config.Bind(Category.gatingCategory, "6. Enable Auto-Gating Debug Overlay", false, new ConfigDescription("DEV SETTING!!! Enables the debug overlay for auto-gating", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            
            // IR illumination
            irFlashlightBrightnessMult = Config.Bind(Category.illuminationCategory, "IR flashlight brightness multiplier", 1.5f, new ConfigDescription("Brightness multiplier for IR flashlights", new AcceptableValueRange<float>(0f, 5f)));
            irFlashlightRangeMult = Config.Bind(Category.illuminationCategory, "IR flashlight range multiplier", 2f, new ConfigDescription("Range multiplier for IR flashlights", new AcceptableValueRange<float>(0f, 10f)));
            irLaserBrightnessMult = Config.Bind(Category.illuminationCategory, "IR laser brightness multiplier", 1f, new ConfigDescription("Brightness multiplier for IR lasers", new AcceptableValueRange<float>(0f, 10f)));
            irLaserRangeMult = Config.Bind(Category.illuminationCategory, "IR laser range multiplier", 1f, new ConfigDescription("Range multiplier for IR lasers", new AcceptableValueRange<float>(0f, 10f)));
            irLaserPointClose = Config.Bind(Category.illuminationCategory, "IR laser point close size multiplier", 1f, new ConfigDescription("Point size multiplier for IR lasers", new AcceptableValueRange<float>(0f, 10f)));
            irLaserPointFar = Config.Bind(Category.illuminationCategory, "IR laser point far size multiplier", 1f, new ConfigDescription("Point size multiplier for IR lasers", new AcceptableValueRange<float>(0f, 10f)));

            irFlashlightBrightnessMult.SettingChanged += (sender, e) => IkLightAwakePatch.UpdateAll();
            irFlashlightRangeMult.SettingChanged += (sender, e) => IkLightAwakePatch.UpdateAll();
            irLaserBrightnessMult.SettingChanged += (sender, e) => LaserBeamAwakePatch.UpdateAll();
            irLaserRangeMult.SettingChanged += (sender, e) => LaserBeamAwakePatch.UpdateAll();
            irLaserPointClose.SettingChanged += (sender, e) => LaserBeamAwakePatch.UpdateAll();
            irLaserPointFar.SettingChanged += (sender, e) => LaserBeamAwakePatch.UpdateAll();

            // other variables.. idk
            gatingLevel.Value = 0;
            
            // load assets
            AssetHelper.LoadShaders();
            AssetHelper.LoadNvgs(Config);
            AssetHelper.LoadThermals(Config);
            AssetHelper.LoadAudioClips();
            
            try
            {
                harmony.PatchAll();

                new NightVisionAwakePatch().Enable();
                new NightVisionApplySettingsPatch().Enable();
                new NightVisionSetMaskPatch().Enable();
                new ThermalVisionSetMaterialPatch().Enable();
                new SprintPatch().Enable();
                new NightVisionMethod_1().Enable(); //reshade
                new InitiateShotPatch().Enable();
                new IkLightAwakePatch().Enable();
                new LaserBeamAwakePatch().Enable();
                new LaserBeamLateUpdatePatch().Enable();
                new EmitGrenadePatch().Enable();
                new GameStartedPatch().Enable();
                AmbientPatch.TogglePatch(!allowAmbientChange.Value);

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
        }

        void Update()
        {
            bool nvgOn = NvgHelper.IsNvgOn;
            if (!nvgOn) return;
            
            if (Input.GetKeyDown(gatingInc.Value) && gatingLevel.Value < 2)
            {
                gatingLevel.Value++;
                Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), AssetHelper.LoadedAudioClips["gatingKnob.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100);
                CameraClass.Instance.NightVision.ApplySettings();
            }
            else if (Input.GetKeyUp(gatingDec.Value) && gatingLevel.Value > -2)
            {
                gatingLevel.Value--;
                Singleton<BetterAudio>.Instance.PlayAtPoint(new Vector3(0, 0, 0), AssetHelper.LoadedAudioClips["gatingKnob.wav"], 0, BetterAudio.AudioSourceGroupType.Nonspatial, 100);
                CameraClass.Instance.NightVision.ApplySettings();
            }
        }

        public static void Log(string message)
        {
            if (!debugLogging.Value) return;

            Logger.LogInfo(message);
        }
    }
}
