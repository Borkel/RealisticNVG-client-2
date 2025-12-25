using BepInEx.Configuration;
using BorkelRNVG.Enum;
using BorkelRNVG.Helpers;
using BorkelRNVG.Struct;

namespace BorkelRNVG.Configuration
{
    public class NightVisionConfig
    {
        // night vision
        public ConfigEntry<float> Gain { get; private set; }
        public ConfigEntry<float> NoiseIntensity { get; private set; }
        public ConfigEntry<float> NoiseSize { get; private set; }
        public ConfigEntry<float> MaskSize { get; private set; }
        public ConfigEntry<float> Red { get; private set; }
        public ConfigEntry<float> Green { get; private set; }
        public ConfigEntry<float> Blue { get; private set; }

        // auto-gating
        public ConfigEntry<EGatingType> AutoGatingType { get; private set; }
        public ConfigEntry<float> GatingSpeed { get; private set; }
        public ConfigEntry<float> MaxBrightness { get; private set; }
        public ConfigEntry<float> MinBrightness { get; private set; }
        public ConfigEntry<float> MinBrightnessThreshold { get; private set; }
        public ConfigEntry<float> MaxBrightnessThreshold { get; private set; }

        // config constructor. all parameters are DEFAULT values
        public NightVisionConfig(ConfigFile config, string category, NightVisionConfigStruct configStruct)
        {
            // night vision
            Gain = config.Bind(category, "1. Gain", configStruct.Gain, new ConfigDescription("Light amplification", new AcceptableValueRange<float>(0f, 5f)));
            NoiseIntensity = config.Bind(category, "2. Noise Intensity", configStruct.NoiseIntensity, new ConfigDescription("Controls the intensity of the noise overlay.", new AcceptableValueRange<float>(0f, 1f)));
            NoiseSize = config.Bind(category, "3. Noise Scale", configStruct.NoiseSize, new ConfigDescription("Controls the scale of the noise pattern.", new AcceptableValueRange<float>(0.01f, 0.99f)));
            MaskSize = config.Bind(category, "4. Mask Size", configStruct.MaskSize, new ConfigDescription("Adjusts the size of the NVG mask.", new AcceptableValueRange<float>(0.01f, 2f)));
            Red = config.Bind(category, "5. Red", configStruct.Red, new ConfigDescription("Adjusts the red color component of the NVG tint.", new AcceptableValueRange<float>(0f, 255f)));
            Green = config.Bind(category, "6. Green", configStruct.Green, new ConfigDescription("Adjusts the green color component of the NVG tint.", new AcceptableValueRange<float>(0f, 255f)));
            Blue = config.Bind(category, "7. Blue", configStruct.Blue, new ConfigDescription("Adjusts the blue color component of the NVG tint.", new AcceptableValueRange<float>(0f, 255f)));

            // auto-gating
            AutoGatingType = config.Bind(category, "8. Adjustment Type", configStruct.GatingType, new ConfigDescription("Enables automatic brightness adjustment for this device. Only used if the global setting is enabled. Off will disable any automatic brightness adjustment. AutoGain will make brightness adjust to ambient light only. AutoGating will also make brightness react to gunshots."));
            GatingSpeed = config.Bind(category, "9. Adjustment Speed", configStruct.GatingSpeed, new ConfigDescription("Changes the rate at which brightness adjusts."));
            MaxBrightness = config.Bind(category, "10. Max Brightness Multiplier", configStruct.MaxBrightness, new ConfigDescription("Changes the maximum brightness multiplier for auto-gating.", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            MinBrightness = config.Bind(category, "11. Min Brightness Multiplier", configStruct.MinBrightness, new ConfigDescription("Changes the minimum brightness multiplier for auto-gating.", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            MinBrightnessThreshold = config.Bind(category, "12. Min Brightness Threshold", configStruct.MinBrightnessThreshold, new ConfigDescription("Changes the minimum brightness level for auto-gating", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            MaxBrightnessThreshold = config.Bind(category, "13. Max Brightness Threshold", configStruct.MaxBrightnessThreshold, new ConfigDescription("Changes the maximum brightness level for auto-gating.", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));

            Gain.SettingChanged += (_, _) => Util.ApplyNightVisionSettings();
            NoiseIntensity.SettingChanged += (_, _) => Util.ApplyNightVisionSettings();
            NoiseSize.SettingChanged += (_, _) => Util.ApplyNightVisionSettings();
            MaskSize.SettingChanged += (_, _) => Util.ApplyNightVisionSettings();
            Red.SettingChanged += (_, _) => Util.ApplyNightVisionSettings();
            Green.SettingChanged += (_, _) => Util.ApplyNightVisionSettings();
            Blue.SettingChanged += (_, _) => Util.ApplyNightVisionSettings();

            AutoGatingType.SettingChanged += (_, _) => Util.ApplyGatingSettings();
            GatingSpeed.SettingChanged += (_, _) => Util.ApplyGatingSettings();
            MaxBrightness.SettingChanged += (_, _) => Util.ApplyGatingSettings();
            MinBrightness.SettingChanged += (_, _) => Util.ApplyGatingSettings();
            MinBrightnessThreshold.SettingChanged += (_, _) => Util.ApplyGatingSettings();
            MaxBrightnessThreshold.SettingChanged += (_, _) => Util.ApplyGatingSettings();
        }
    }
}
