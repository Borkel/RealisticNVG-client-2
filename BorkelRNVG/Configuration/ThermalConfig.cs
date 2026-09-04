using BepInEx.Configuration;
using BorkelRNVG.Enum;
using BorkelRNVG.Helpers;
using BorkelRNVG.Struct;

namespace BorkelRNVG.Configuration
{
    public class ThermalConfig
    {
        public ConfigEntry<bool> IsFpsStuck { get; private set; }
        public ConfigEntry<int> MinFps { get; private set; }
        public ConfigEntry<int> MaxFps { get; private set; }
        public ConfigEntry<bool> IsPixelated { get; private set; }
        public ConfigEntry<int> VerticalResolution { get; private set; }
        public ConfigEntry<bool> IsNoisy { get; private set; }
        public ConfigEntry<bool> IsMotionBlurred { get; private set; }

        // config constructor. all parameters are DEFAULT values
        public ThermalConfig(ConfigFile config, string category, ThermalConfigStruct configStruct)
        {
            IsFpsStuck = config.Bind(category, "1. Enable FPS Stuck", configStruct.IsFpsStuck, new ConfigDescription("Enables FPS limiter for this device"));
            MinFps = config.Bind(category, "2. Minimum FPS", configStruct.MinFps, new ConfigDescription("Minimum FPS for this device"));
            MaxFps = config.Bind(category, "3. Maximum FPS", configStruct.MaxFps, new ConfigDescription("Maximum FPS for this device"));
            IsPixelated = config.Bind(category, "4. Pixelated", configStruct.IsPixelated, new ConfigDescription("Sets whether the device's vision should be pixelated"));
            VerticalResolution = config.Bind(
                category,
                "5. Vertical thermal resolution",
                configStruct.VerticalResolution,
                new ConfigDescription(
                    "Vertical pixel resolution inside the T-7 display area. Block count is adjusted automatically for the current screen aspect ratio.",
                    new AcceptableValueRange<int>(16, 2160)));
            IsNoisy = config.Bind(category, "6. Noisy", configStruct.IsNoisy, new ConfigDescription("Sets whether the device's vision should be noisy"));
            IsMotionBlurred = config.Bind(category, "7. Motion Blurred", configStruct.IsMotionBlurred, new  ConfigDescription("Sets whether the device's vision should have motion blur"));
        }
    }
}
