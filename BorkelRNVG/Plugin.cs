using BepInEx;
using BepInEx.Configuration;
using BorkelRNVG.Patches;
using System;
using System.Collections.Generic;
using BepInEx.Logging;
using BorkelRNVG.Helpers;
using BorkelRNVG.Controllers;
using HarmonyLib;
using UnityEngine;

namespace BorkelRNVG
{
    [BepInPlugin("com.borkel.nvgmasks", "Borkel's Realistic NVGs", "2.3.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static new ManualLogSource Logger;
        public static Harmony harmony = new Harmony("com.borkel.nvgmasks");
        
        public static ConfigEntry<bool> debugLogging;

        // global
        public static ConfigEntry<float> globalMaskSize;
        public static ConfigEntry<float> globalGain;
        public static ConfigEntry<bool> allowAmbientChange;

        // manual gain
        public static ConfigEntry<KeyboardShortcut> manualGainIncrease;
        public static ConfigEntry<KeyboardShortcut> manualGainDecrease;
        public static ConfigEntry<float> manualGainSpeed;

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
            globalGain = Config.Bind(Category.globalCategory, "2. Gain multiplier", 1f, new ConfigDescription("Final visual gain multiplier used to compensate for display or post-processing settings.", new AcceptableValueRange<float>(0f, 5f)));
            globalGain.SettingChanged += (_, _) => NvgHelper.ApplyNightVisionSettings();
            allowAmbientChange = Config.Bind(
                Category.globalCategory,
                "3. Allow ambient change",
                false,
                new ConfigDescription(
                    "Toggles whether night vision affects ambient lighting.",
                    null,
                    new ConfigurationManagerAttributes
                    {
                        Browsable = false
                    }));

            allowAmbientChange.Value = false;
            allowAmbientChange.SettingChanged += (sender, e) => AmbientPatch.TogglePatch(!allowAmbientChange.Value);

            // Manual gain
            manualGainIncrease = Config.Bind(
                Category.gainControlCategory,
                "1. Increase manual gain",
                new KeyboardShortcut(KeyCode.None),
                "Hold to restore the maximum exposure available to automatic gain.");
            manualGainDecrease = Config.Bind(
                Category.gainControlCategory,
                "2. Decrease manual gain",
                new KeyboardShortcut(KeyCode.None),
                "Hold to lower the maximum exposure available to automatic gain.");
            manualGainSpeed = Config.Bind(
                Category.gainControlCategory,
                "3. Manual gain speed",
                2f,
                new ConfigDescription(
                    "Continuous manual gain adjustment speed in EV per second.",
                    new AcceptableValueRange<float>(0.05f, 10f)));
            
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

            // load assets
            AssetHelper.LoadShaders();
            AssetHelper.LoadLensLayouts(Config);
            AssetHelper.LoadNvgs(Config);
            AssetHelper.LoadThermals(Config);
            AssetHelper.LoadAudioClips();
            
            try
            {
                harmony.PatchAll();

                new NightVisionAwakePatch().Enable();
                new NightVisionApplySettingsPatch().Enable();
                new NightVisionRenderPatch().Enable();
                new NightVisionSetMaskPatch().Enable();
                new ThermalVisionSetMaterialPatch().Enable();
                new SprintPatch().Enable();
                new NightVisionSwitchPatch().Enable(); //reshade
                new IkLightAwakePatch().Enable();
                new LaserBeamAwakePatch().Enable();
                new LaserBeamLateUpdatePatch().Enable();
                new GameStartedPatch().Enable();
                bool disableAmbientChange = !allowAmbientChange.Value;
                AmbientPatch.TogglePatch(disableAmbientChange);

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

        private void Update()
        {
            if (!NvgHelper.IsNvgOn)
                return;

            RealisticNightVisionRenderer renderer =
                CameraClass.Instance?.NightVision?.GetComponent<RealisticNightVisionRenderer>();
            if (renderer == null || !renderer.ManualGainControlEnabled)
                return;

            float direction = 0f;
            if (manualGainIncrease.Value.IsPressed())
                direction += 1f;
            if (manualGainDecrease.Value.IsPressed())
                direction -= 1f;

            if (!Mathf.Approximately(direction, 0f))
                renderer.AdjustManualExposureEV(
                    direction * manualGainSpeed.Value * Time.unscaledDeltaTime);
        }

        public static void Log(string message)
        {
            if (!debugLogging.Value) return;

            Logger.LogInfo(message);
        }
    }
}
