using BepInEx.Configuration;
using BorkelRNVG.Helpers.Enum;

namespace BorkelRNVG.Helpers.Configuration
{
    public class NightVisionConfig
    {
        // night vision
        public ConfigEntry<float> Gain;
        public ConfigEntry<float> NoiseIntensity;
        public ConfigEntry<float> NoiseSize;
        public ConfigEntry<float> MaskSize;
        public ConfigEntry<float> Red;
        public ConfigEntry<float> Green;
        public ConfigEntry<float> Blue;

        // auto-gating
        public ConfigEntry<EGatingType> AutoGatingType;
        public ConfigEntry<float> GatingSpeed;
        public ConfigEntry<float> MaxBrightness;
        public ConfigEntry<float> MinBrightness;
        public ConfigEntry<float> MinBrightnessThreshold;
        public ConfigEntry<float> MaxBrightnessThreshold;

        //lens distortion
        public ConfigEntry<float> EdgeDistortion;
        public ConfigEntry<float> EdgeDistortionStart;

        // config constructor. all parameters are DEFAULT values
        public NightVisionConfig(ConfigFile config, string category,
            float gain, float noiseIntensity, float noiseSize, float maskSize,
            float red, float green, float blue,
            EGatingType gatingType, float gatingSpeed, float maxBrightness,
            float minBrightness, float minBrightnessThreshold, float maxBrightnessThreshold, float edgeDistortion, float edgeDistortionStart)
        {
            // night vision
            Gain = config.Bind(category, "1. Gain", gain, new ConfigDescription("Light amplification", new AcceptableValueRange<float>(0f, 5f)));
            NoiseIntensity = config.Bind(category, "2. Noise Intensity", noiseIntensity, new ConfigDescription("Controls the intensity of the noise overlay.", new AcceptableValueRange<float>(0f, 1f)));
            NoiseSize = config.Bind(category, "3. Noise Scale", noiseSize, new ConfigDescription("Controls the scale of the noise pattern.", new AcceptableValueRange<float>(0.01f, 0.99f)));
            MaskSize = config.Bind(category, "4. Mask Size", maskSize, new ConfigDescription("Adjusts the size of the NVG mask.", new AcceptableValueRange<float>(0.01f, 2f)));
            Red = config.Bind(category, "5. Red", red, new ConfigDescription("Adjusts the red color component of the NVG tint.", new AcceptableValueRange<float>(0f, 255f)));
            Green = config.Bind(category, "6. Green", green, new ConfigDescription("Adjusts the green color component of the NVG tint.", new AcceptableValueRange<float>(0f, 255f)));
            Blue = config.Bind(category, "7. Blue", blue, new ConfigDescription("Adjusts the blue color component of the NVG tint.", new AcceptableValueRange<float>(0f, 255f)));

            // auto-gating
            AutoGatingType = config.Bind(category, "8. Adjustment Type", gatingType, new ConfigDescription("EXPERIMENTAL, WILL REDUCE FPS! Enables automatic brightness adjustment for night vision devices. Off will disable any automatic brightness adjustment. AutoGain will make brightness adjust to ambient light only. AutoGating will also make brightness react to gunshots."));
            GatingSpeed = config.Bind(category, "9. Adjustment Speed", gatingSpeed, new ConfigDescription("Changes the rate at which brightness adjusts."));
            MaxBrightness = config.Bind(category, "10. Max Brightness Multiplier", maxBrightness, new ConfigDescription("Changes the maximum brightness multiplier for auto-gating.", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            MinBrightness = config.Bind(category, "11. Min Brightness Multiplier", minBrightness, new ConfigDescription("Changes the minimum brightness multiplier for auto-gating.", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            MinBrightnessThreshold = config.Bind(category, "12. Min Brightness Threshold", minBrightnessThreshold, new ConfigDescription("Changes the minimum brightness level for auto-gating", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));
            MaxBrightnessThreshold = config.Bind(category, "13. Max Brightness Threshold", maxBrightnessThreshold, new ConfigDescription("Changes the maximum brightness level for auto-gating.", null, new ConfigurationManagerAttributes() { IsAdvanced = true }));

            //lens distortion
            EdgeDistortion = config.Bind(category, "14. Edge distortion amount", edgeDistortion, new ConfigDescription("Adjusts the amount of distortion around the lens.", new AcceptableValueRange<float>(0f, 1f)));
            EdgeDistortionStart = config.Bind(category, "15. Edge distortion radius", edgeDistortionStart, new ConfigDescription("Adjusts the starting point of the distortion.", new AcceptableValueRange<float>(0f, 0.55f)));

            Gain.SettingChanged += Util.ApplyNightVisionSettings;
            NoiseIntensity.SettingChanged += Util.ApplyNightVisionSettings;
            NoiseSize.SettingChanged += Util.ApplyNightVisionSettings;
            MaskSize.SettingChanged += Util.ApplyNightVisionSettings;
            Red.SettingChanged += Util.ApplyNightVisionSettings;
            Green.SettingChanged += Util.ApplyNightVisionSettings;
            Blue.SettingChanged += Util.ApplyNightVisionSettings;
            EdgeDistortion.SettingChanged += Util.ApplyNightVisionSettings;
            EdgeDistortionStart.SettingChanged += Util.ApplyNightVisionSettings;

            AutoGatingType.SettingChanged += Util.ApplyGatingSettings;
            GatingSpeed.SettingChanged += Util.ApplyGatingSettings;
            MaxBrightness.SettingChanged += Util.ApplyGatingSettings;
            MinBrightness.SettingChanged += Util.ApplyGatingSettings;
            MinBrightnessThreshold.SettingChanged += Util.ApplyGatingSettings;
            MaxBrightnessThreshold.SettingChanged += Util.ApplyGatingSettings;
        }
    }
}

