namespace BorkelRNVG.Models
{
    public sealed class RealisticNvgSettings
    {
        public float PhosphorRed { get; set; } = 0.62f;
        public float PhosphorGreen { get; set; } = 0.92f;
        public float PhosphorBlue { get; set; } = 0.98f;
        public float SpectralSensitivityRed { get; set; } = 0.45f;
        public float SpectralSensitivityGreen { get; set; } = 0.45f;
        public float SpectralSensitivityBlue { get; set; } = 0.10f;

        public bool AutoExposure { get; set; } = true;
        public bool ManualGainControl { get; set; }
        public float TargetLuminance { get; set; } = 0.18f;
        public float MinimumExposureEV { get; set; } = 0f;
        public float MaximumExposureEV { get; set; } = 3.432959f;
        public float BrightSceneSpeed { get; set; } = 10f;
        public float DarkSceneSpeed { get; set; } = 2f;
        public float HighlightProtection { get; set; } = 1f;

        public bool NearDepthOfField { get; set; } = true;
        public float FullBlurDistance { get; set; } = 0.35f;
        public float SharpDistance { get; set; } = 5f;
        public float NearBlurStrength { get; set; } = 1f;
        public float NearBlurRadiusPixels { get; set; } = 16f;

        public bool OpticalHaze { get; set; } = true;
        public float HazeBlurRadiusPixels { get; set; } = 6f;
        public float HazeCenterStrength { get; set; } = 0.1f;
        public float HazeEdgeStrength { get; set; } = 0.8f;
        public float HazeEdgeWidthPixels { get; set; } = 90f;
        public float HazeFalloff { get; set; } = 1.5f;
        public float HazeVeilStrength { get; set; } = 0.08f;
        public float ChromaticAberrationPixels { get; set; } = 1.25f;

        public bool Bloom { get; set; }
        public float BloomThreshold { get; set; }
        public float BloomSoftKnee { get; set; } = 0.5f;
        public float BloomIntensity { get; set; } = 0.13f;
        public float BloomRadiusPixels { get; set; } = 22f;
        public float WideBloomRadiusPixels { get; set; } = 80f;

        public float ReadNoise { get; set; } = 0.03f;
        public float ShotNoise { get; set; } = 0.065f;
        public float CoarseNoise { get; set; } = 0.022f;
        public float NoiseIntensity { get; set; } = 1f;
        public float FixedPatternNoise { get; set; } = 0.018f;
        public float Scintillation { get; set; } = 0.5f;
        public float ScintillationDensity { get; set; } = 0.1f;
        public float NoiseFadeStartLuminance { get; set; }
        public float NoiseFadeEndLuminance { get; set; } = 0.03f;
        public float GrainPixelSize { get; set; } = 1f;
        public float NoiseRefreshRate { get; set; } = 30f;

        public float OpticScale { get; set; } = 1f;
        public string LensLayout { get; set; } = "monocular";
        public bool EdgeDistortion { get; set; } = true;
        public float DistortionStrengthPixels { get; set; } = 6f;
        public float DistortionWidthPixels { get; set; } = 60f;
        public float DistortionFalloff { get; set; } = 2f;
        public float SeamWidth { get; set; } = 0.006f;
        public float SeamOpacity { get; set; } = 0.65f;
        public bool Vignette { get; set; } = true;
        public float VignetteWidth { get; set; } = 0.12f;
        public float VignetteStrength { get; set; } = 0.20f;
        public float VignetteFalloff { get; set; } = 1.35f;

        public RealisticNvgSettings Clone()
        {
            return (RealisticNvgSettings)MemberwiseClone();
        }
    }
}
