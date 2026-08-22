using System;
using BepInEx.Configuration;
using BorkelRNVG.Helpers;
using BorkelRNVG.Models;

namespace BorkelRNVG.Configuration
{
    public sealed class LensLayoutConfig
    {
        private const int MaximumLenses = 4;
        private int order = 2000;

        public LensLayoutDefinition Values { get; }

        public LensLayoutConfig(ConfigFile config, LensLayoutDefinition defaults)
        {
            Values = (defaults ?? new LensLayoutDefinition()).Clone();
            Values.Lenses ??= [];
            while (Values.Lenses.Count < MaximumLenses)
                Values.Lenses.Add(new LensLayoutTube { Enabled = false });

            string displayName = string.IsNullOrWhiteSpace(Values.DisplayName)
                ? Values.Id
                : Values.DisplayName;
            string category = "Lens Layout - " + displayName;

            for (int index = 0; index < MaximumLenses; index++)
                BindLens(config, category, index, Values.Lenses[index]);
        }

        private void BindLens(ConfigFile config, string category, int index,
            LensLayoutTube lens)
        {
            int prefix = (index + 1) * 10;
            string label = "Tube " + (index + 1);
            Bind(config, category, prefix + " " + label + " - Enabled",
                lens.Enabled, "Include this tube in the layout.",
                value => lens.Enabled = value);
            Bind(config, category, (prefix + 1) + " " + label + " - Center X",
                lens.CenterX, "Horizontal center in optic-texture UV coordinates.",
                value => lens.CenterX = value, Range(-1f, 2f));
            Bind(config, category, (prefix + 2) + " " + label + " - Center Y",
                lens.CenterY, "Vertical center in optic-texture UV coordinates.",
                value => lens.CenterY = value, Range(-1f, 2f));
            Bind(config, category, (prefix + 3) + " " + label + " - Radius",
                lens.Radius, "Tube radius measured in optic-texture heights.",
                value => lens.Radius = value, Range(0f, 2f));
            Bind(config, category, (prefix + 4) + " " + label + " - Distortion multiplier",
                lens.DistortionMultiplier, "Multiplier for edge distortion on this tube.",
                value => lens.DistortionMultiplier = value, Range(-4f, 4f));
            Bind(config, category, (prefix + 5) + " " + label + " - Fusion group",
                lens.FusionGroup,
                "Overlapping tubes in the same group fuse visually; different groups keep a physical boundary.",
                value => lens.FusionGroup = value,
                new AcceptableValueRange<int>(0, 3));
            Bind(config, category, (prefix + 6) + " " + label + " - Vignette multiplier",
                lens.VignetteMultiplier, "Multiplier for edge vignette on this tube.",
                value => lens.VignetteMultiplier = value, Range(0f, 4f));
        }

        private void Bind<T>(ConfigFile config, string category, string key,
            T defaultValue, string description, Action<T> setter,
            AcceptableValueBase acceptable = null)
        {
            ConfigEntry<T> entry = config.Bind(category, key, defaultValue,
                new ConfigDescription(description, acceptable,
                    new ConfigurationManagerAttributes
                    {
                        IsAdvanced = true,
                        Order = order -= 10
                    }));
            setter(entry.Value);
            entry.SettingChanged += (_, _) =>
            {
                setter(entry.Value);
                NvgHelper.ApplyNightVisionSettings();
            };
        }

        private static AcceptableValueRange<float> Range(float minimum, float maximum)
        {
            return new AcceptableValueRange<float>(minimum, maximum);
        }
    }
}
