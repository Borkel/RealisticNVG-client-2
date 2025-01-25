﻿using BepInEx.Configuration;
using BorkelRNVG.Helpers.Configuration;
using System;
using System.Collections.Generic;
using UnityEngine;
using WindowsInput.Native;
using static GClass1907;

namespace BorkelRNVG.Helpers.Configuration
{
    // feel like this shit is way too over-engineered
    public class NightVisionItemConfig
    {
        public NightVisionConfig NightVisionConfig { get; set; }
        public float Intensity { get; set; }
        public float NoiseIntensity { get; set; }
        public float NoiseScale { get; set; }
        public float MaskSize { get; set; }
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }
        public VirtualKeyCode Key { get; set; }
        public Texture2D BinocularMaskTexture { get; set; }
        public Action Update { get; set; }

        // am i abusing delegates too hard? probably not.. just feels weird
        // is this too much voodoo
        public NightVisionItemConfig(
            NightVisionConfig nvgConfig,
            Func<float> intensityCalc,
            Func<float> noiseIntensityCalc,
            Func<float> noiseScaleCalc,
            Func<float> maskSizeCalc,
            Func<float> r,
            Func<float> g,
            Func<float> b,
            VirtualKeyCode key,
            Texture2D binocularMaskTexture)
        {
            // define the update action
            Update = () =>
            {
                float gatingValue = AutoGatingController.Instance == null ? 1 : AutoGatingController.Instance.GatingMultiplier;

                Intensity = intensityCalc() * gatingValue;
                NoiseIntensity = noiseIntensityCalc();
                NoiseScale = noiseScaleCalc();
                MaskSize = maskSizeCalc();
                R = r();
                G = g();
                B = b();
                Key = key;
                BinocularMaskTexture = binocularMaskTexture;
                NightVisionConfig = nvgConfig;
            };

            // initial calculation
            Update();
        }

        public static Dictionary<string, NightVisionItemConfig> Configs = new();

        public static NightVisionItemConfig Add(string itemId, NightVisionItemConfig config)
        {
            if (Configs.ContainsKey(itemId))
                return Configs[itemId];

            Configs.Add(itemId, config);
            return config;
        }

        public static void Remove(string itemId) // but why would you use it..? idk
        {
            if (Configs.ContainsKey(itemId))
            {
                Configs.Remove(itemId);
            }
        }

        public static NightVisionItemConfig Get(string itemId)
        {
            Configs.TryGetValue(itemId, out NightVisionItemConfig config);
            return config;
        }

        public static void InitializeNVGs(ConfigFile configFile)
        {
            /* 
            NightVisionConfig values:
            configFile, itemId, 
            2.5f, 0.2f, 0.1f, 0.96f, 152f, 214f, 252f, 
            true, 0.3f, 1f, 0.2f, 0f, 0.15f

            // 1.configfile, 2.category, 3.itemId
            // 1.gain, 2.noiseIntensity, 3.noiseSize, 4.maskSize, 5.red, 6.green, 7.blue  
            // 1.gatingEnabled, 2.gatingSpeed, 3.maxBrightness, 4.minBrightness, 5.maxThreshold, 6.minThreshold
                
            NightVisionItemConfig delegates
            Func<float> intensityCalc,
            Func<float> noiseIntensityCalc,
            Func<float> noiseScaleCalc,
            Func<float> maskSizeCalc,
            Func<float> r,
            Func<float> g,
            Func<float> b,
             */

            // vanilla GPNVG-18
            string gpnvg18 = "5c0558060db834001b735271";
            NightVisionConfig gpnvgConfig = new NightVisionConfig(
                    configFile, Plugin.gpnvgCategory,
                    2.5f, 0.2f, 0.1f, 0.96f, 152f, 214f, 252f,
                    true, 0.3f, 1f, 0.2f, 0f, 0.15f
                );
            Add(gpnvg18, new NightVisionItemConfig(
                gpnvgConfig,
                () => gpnvgConfig.Gain.Value * Plugin.globalGain.Value + gpnvgConfig.Gain.Value * Plugin.globalGain.Value * 0.3f * Plugin.gatingLevel.Value / 2,
                () => 2 * gpnvgConfig.NoiseIntensity.Value,
                () => 2f - 2 * gpnvgConfig.NoiseSize.Value,
                () => gpnvgConfig.MaskSize.Value * Plugin.globalMaskSize.Value,
                () => gpnvgConfig.Red.Value / 255,
                () => gpnvgConfig.Green.Value / 255,
                () => gpnvgConfig.Blue.Value / 255,
                VirtualKeyCode.NUMPAD9,
                null
            ));

            // artem nvgs
            Add("66326bfd46817c660d01512e", Get(gpnvg18));
            Add("66326bfd46817c660d015148", Get(gpnvg18));
            Add("66326bfd46817c660d015146", Get(gpnvg18));

            // PVS-14
            string pvs14 = "57235b6f24597759bf5a30f1";
            var pvs14Config = new NightVisionConfig(
                    configFile, Plugin.pvsCategory,
                    2.4f, 0.2f, 0.1f, 1f, 95f, 210f, 255f,
                    true, 0.3f, 1f, 0.2f, 0f, 0.15f
                );
            Add(pvs14, new NightVisionItemConfig(
                pvs14Config,
                () => pvs14Config.Gain.Value * Plugin.globalGain.Value + pvs14Config.Gain.Value * Plugin.globalGain.Value * 0.3f * Plugin.gatingLevel.Value / 2,
                () => 2 * pvs14Config.NoiseIntensity.Value,
                () => 2f - 2 * pvs14Config.NoiseSize.Value,
                () => pvs14Config.MaskSize.Value * Plugin.globalMaskSize.Value,
                () => pvs14Config.Red.Value / 255,
                () => pvs14Config.Green.Value / 255,
                () => pvs14Config.Blue.Value / 255,
                VirtualKeyCode.NUMPAD8,
                null
            ));

            // N-15
            string n15 = "5c066e3a0db834001b7353f0";
            NightVisionConfig n15Config = new NightVisionConfig(
                    configFile, Plugin.nCategory,
                    2.1f, 0.25f, 0.15f, 1f, 60f, 235f, 100f,
                    true, 0.3f, 1f, 0.2f, 0f, 0.15f
                );
            Add(n15, new NightVisionItemConfig(
                n15Config,
                () => n15Config.Gain.Value * Plugin.globalGain.Value + n15Config.Gain.Value * Plugin.globalGain.Value * 0.3f * Plugin.gatingLevel.Value / 2,
                () => 2 * n15Config.NoiseIntensity.Value,
                () => 2f - 2 * n15Config.NoiseSize.Value,
                () => n15Config.MaskSize.Value * Plugin.globalMaskSize.Value,
                () => n15Config.Red.Value / 255,
                () => n15Config.Green.Value / 255,
                () => n15Config.Blue.Value / 255,
                VirtualKeyCode.NUMPAD7,
                null
            ));

            // PNV-10T
            string pnv10t = "5c0696830db834001d23f5da";
            NightVisionConfig pnv10Config = new NightVisionConfig(
                    configFile, Plugin.pnvCategory,
                    1.8f, 0.3f, 0.2f, 1f, 60f, 210f, 60f,
                    true, 0.3f, 1f, 0.2f, 0f, 0.15f
                );
            Add(pnv10t, new NightVisionItemConfig(
                pnv10Config,
                () => pnv10Config.Gain.Value * Plugin.globalGain.Value + pnv10Config.Gain.Value * Plugin.globalGain.Value * 0.3f * Plugin.gatingLevel.Value / 2,
                () => 2 * pnv10Config.NoiseIntensity.Value,
                () => 2f - 2 * pnv10Config.NoiseSize.Value,
                () => pnv10Config.MaskSize.Value * Plugin.globalMaskSize.Value,
                () => pnv10Config.Red.Value / 255,
                () => pnv10Config.Green.Value / 255,
                () => pnv10Config.Blue.Value / 255,
                VirtualKeyCode.NUMPAD6,
                null
            ));

            Plugin.t7Pixelation = configFile.Bind(Plugin.t7Category, "1. Pixelation", true, "Requires restart. Pixelates the T-7, like a real digital screen");
            Plugin.t7HzLock = configFile.Bind(Plugin.t7Category, "2. Hz lock", true, "Requires restart. Locks the Hz of the T-7 to 60Hz, like a real digital screen");
        }
    }
}
