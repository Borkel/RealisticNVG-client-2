using System;
using BepInEx.Configuration;
using BorkelRNVG.Helpers;
using BorkelRNVG.Models;

namespace BorkelRNVG.Configuration
{
    public sealed class NightVisionConfig
    {
        private int order = 2000;

        public RealisticNvgSettings Values { get; }

        public NightVisionConfig(ConfigFile config, string category,
            RealisticNvgSettings defaults, bool exposeSettings)
        {
            Values = (defaults ?? new RealisticNvgSettings()).Clone();
            if (!exposeSettings)
                return;

            Bind(config, category, "10 Tube - Phosphor red", Values.PhosphorRed,
                "Red component of the single phosphor color.", value => Values.PhosphorRed = value, Range(0f, 1f));
            Bind(config, category, "11 Tube - Phosphor green", Values.PhosphorGreen,
                "Green component of the single phosphor color.", value => Values.PhosphorGreen = value, Range(0f, 1f));
            Bind(config, category, "12 Tube - Phosphor blue", Values.PhosphorBlue,
                "Blue component of the single phosphor color.", value => Values.PhosphorBlue = value, Range(0f, 1f));
            Bind(config, category, "13 Tube - Manual gain", Values.ManualGain,
                "Base linear gain before automatic exposure.", value => Values.ManualGain = value, Range(0f, 5f));
            Bind(config, category, "14 Tube - Response gamma", Values.ResponseGamma,
                "Shapes the tube response; lower values lift midtones.", value => Values.ResponseGamma = value, Range(0.05f, 3f));
            Bind(config, category, "15 Tube - White point", Values.WhitePoint,
                "Lower values make the tube saturate sooner.", value => Values.WhitePoint = value, Range(0.01f, 4f));
            Bind(config, category, "16 Tube - Black level", Values.BlackLevel,
                "Input luminance removed before amplification.", value => Values.BlackLevel = value, Range(0f, 0.2f));

            Bind(config, category, "20 Exposure - Enabled", Values.AutoExposure,
                "Enable scene-luminance-driven automatic gain.", value => Values.AutoExposure = value);
            Bind(config, category, "21 Exposure - Target luminance", Values.TargetLuminance,
                "Average aperture luminance targeted by automatic gain.", value => Values.TargetLuminance = value, Range(0.001f, 2f));
            Bind(config, category, "22 Exposure - Minimum EV", Values.MinimumExposureEV,
                "Lowest automatic exposure correction in bright scenes.", value => Values.MinimumExposureEV = value, Range(-10f, 10f));
            Bind(config, category, "23 Exposure - Maximum EV", Values.MaximumExposureEV,
                "Highest automatic exposure correction in dark scenes.", value => Values.MaximumExposureEV = value, Range(-10f, 12f));
            Bind(config, category, "24 Exposure - Bright scene speed", Values.BrightSceneSpeed,
                "Speed at which gain is reduced.", value => Values.BrightSceneSpeed = value, Range(0f, 30f));
            Bind(config, category, "25 Exposure - Dark scene speed", Values.DarkSceneSpeed,
                "Speed at which gain recovers in darkness.", value => Values.DarkSceneSpeed = value, Range(0f, 30f));
            Bind(config, category, "26 Exposure - Highlight protection", Values.HighlightProtection,
                "Makes bright pixels influence metering more strongly.", value => Values.HighlightProtection = value, Range(0f, 1f));

            Bind(config, category, "30 Near focus - Enabled", Values.NearDepthOfField,
                "Blur objects closer than the focus distance.", value => Values.NearDepthOfField = value);
            Bind(config, category, "31 Near focus - Full blur distance", Values.FullBlurDistance,
                "Distance at which near blur is strongest.", value => Values.FullBlurDistance = value, Range(0.01f, 10f));
            Bind(config, category, "32 Near focus - Sharp distance", Values.SharpDistance,
                "Distance at which the scene becomes sharp.", value => Values.SharpDistance = value, Range(0.02f, 30f));
            Bind(config, category, "33 Near focus - Strength", Values.NearBlurStrength,
                "Blend strength of near-object blur.", value => Values.NearBlurStrength = value, Range(0f, 2f));
            Bind(config, category, "34 Near focus - Radius pixels", Values.NearBlurRadiusPixels,
                "Full-resolution blur radius.", value => Values.NearBlurRadiusPixels = value, Range(0f, 64f));

            Bind(config, category, "40 Haze - Enabled", Values.OpticalHaze,
                "Enable edge haze and optical veil.", value => Values.OpticalHaze = value);
            Bind(config, category, "41 Haze - Blur radius pixels", Values.HazeBlurRadiusPixels,
                "Blur radius used by the optical haze.", value => Values.HazeBlurRadiusPixels = value, Range(0f, 32f));
            Bind(config, category, "42 Haze - Center strength", Values.HazeCenterStrength,
                "Haze strength near the center of each tube.", value => Values.HazeCenterStrength = value, Range(0f, 2f));
            Bind(config, category, "43 Haze - Edge strength", Values.HazeEdgeStrength,
                "Haze strength near tube edges.", value => Values.HazeEdgeStrength = value, Range(0f, 2f));
            Bind(config, category, "44 Haze - Edge width pixels", Values.HazeEdgeWidthPixels,
                "Width of the edge-haze region.", value => Values.HazeEdgeWidthPixels = value, Range(0f, 400f));
            Bind(config, category, "45 Haze - Falloff", Values.HazeFalloff,
                "Shape of the edge-haze transition.", value => Values.HazeFalloff = value, Range(0.05f, 8f));
            Bind(config, category, "46 Haze - Veil strength", Values.HazeVeilStrength,
                "Additional broad light veil near tube edges.", value => Values.HazeVeilStrength = value, Range(0f, 2f));
            Bind(config, category, "47 Haze - Chromatic aberration pixels", Values.ChromaticAberrationPixels,
                "Red/blue separation near tube edges.", value => Values.ChromaticAberrationPixels = value, Range(-8f, 8f));

            Bind(config, category, "50 Bloom - Enabled", Values.Bloom,
                "Enable phosphor glow around bright sources.", value => Values.Bloom = value);
            Bind(config, category, "51 Bloom - Threshold", Values.BloomThreshold,
                "Input luminance required to generate bloom.", value => Values.BloomThreshold = value, Range(0f, 4f));
            Bind(config, category, "52 Bloom - Soft knee", Values.BloomSoftKnee,
                "Softness around the bloom threshold.", value => Values.BloomSoftKnee = value, Range(0f, 1f));
            Bind(config, category, "53 Bloom - Intensity", Values.BloomIntensity,
                "Strength of phosphor glow.", value => Values.BloomIntensity = value, Range(0f, 3f));
            Bind(config, category, "54 Bloom - Radius pixels", Values.BloomRadiusPixels,
                "Radius of the main glow.", value => Values.BloomRadiusPixels = value, Range(0f, 100f));
            Bind(config, category, "55 Bloom - Wide radius pixels", Values.WideBloomRadiusPixels,
                "Radius of the broad secondary glow.", value => Values.WideBloomRadiusPixels = value, Range(0f, 300f));

            Bind(config, category, "60 Noise - Read", Values.ReadNoise,
                "Baseline electronic noise.", value => Values.ReadNoise = value, Range(0f, 0.5f));
            Bind(config, category, "61 Noise - Shot", Values.ShotNoise,
                "Photon-statistics noise.", value => Values.ShotNoise = value, Range(0f, 0.5f));
            Bind(config, category, "62 Noise - Coarse", Values.CoarseNoise,
                "Large-scale procedural noise.", value => Values.CoarseNoise = value, Range(0f, 0.5f));
            Bind(config, category, "63 Noise - Fixed pattern", Values.FixedPatternNoise,
                "Stationary spatial gain variation.", value => Values.FixedPatternNoise = value, Range(0f, 0.5f));
            Bind(config, category, "64 Noise - Scintillation strength", Values.Scintillation,
                "Brightness of scintillation events.", value => Values.Scintillation = value, Range(0f, 1f));
            Bind(config, category, "65 Noise - Scintillation density", Values.ScintillationDensity,
                "Number of one-to-three-pixel scintillation events; zero disables them.", value => Values.ScintillationDensity = value, Range(0f, 10f));
            Bind(config, category, "66 Noise - Grain pixel size", Values.GrainPixelSize,
                "Pixel size of fine and coarse noise; scintillation size is independent.", value => Values.GrainPixelSize = value, Range(0.5f, 8f));
            Bind(config, category, "67 Noise - Refresh rate", Values.NoiseRefreshRate,
                "Distinct procedural noise frames per second.", value => Values.NoiseRefreshRate = value, Range(1f, 240f));

            Bind(config, category, "70 Optics - Scale", Values.OpticScale,
                "Scale of lens and housing textures.", value => Values.OpticScale = value, Range(0.25f, 2f));
            Bind(config, category, "71 Optics - Edge distortion", Values.EdgeDistortion,
                "Enable distortion around individual tube edges.", value => Values.EdgeDistortion = value);
            Bind(config, category, "72 Optics - Distortion strength pixels", Values.DistortionStrengthPixels,
                "Radial displacement at tube edges.", value => Values.DistortionStrengthPixels = value, Range(-32f, 32f));
            Bind(config, category, "73 Optics - Distortion width pixels", Values.DistortionWidthPixels,
                "Width of the distorted edge region.", value => Values.DistortionWidthPixels = value, Range(0.01f, 300f));
            Bind(config, category, "74 Optics - Distortion falloff", Values.DistortionFalloff,
                "Shape of edge-distortion decay.", value => Values.DistortionFalloff = value, Range(0.05f, 8f));
            Bind(config, category, "75 Optics - Seam width", Values.SeamWidth,
                "Width of boundaries between GPNVG tubes.", value => Values.SeamWidth = value, Range(0f, 0.1f));
            Bind(config, category, "76 Optics - Seam opacity", Values.SeamOpacity,
                "Darkness of boundaries between GPNVG tubes.", value => Values.SeamOpacity = value, Range(0f, 1f));
            Bind(config, category, "77 Optics - Vignette", Values.Vignette,
                "Enable per-tube outer vignette.", value => Values.Vignette = value);
            Bind(config, category, "78 Optics - Vignette width", Values.VignetteWidth,
                "Width of per-tube edge darkening.", value => Values.VignetteWidth = value, Range(0.001f, 1f));
            Bind(config, category, "79 Optics - Vignette strength", Values.VignetteStrength,
                "Strength of per-tube edge darkening.", value => Values.VignetteStrength = value, Range(0f, 1f));
            Bind(config, category, "80 Optics - Vignette falloff", Values.VignetteFalloff,
                "Shape of per-tube edge darkening.", value => Values.VignetteFalloff = value, Range(0.05f, 8f));
        }

        private void Bind<T>(ConfigFile config, string category, string key,
            T defaultValue, string description, Action<T> setter,
            AcceptableValueBase acceptable = null)
        {
            ConfigEntry<T> entry = config.Bind(category, key, defaultValue,
                new ConfigDescription(description, acceptable,
                    new ConfigurationManagerAttributes { Order = order -= 10 }));
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
