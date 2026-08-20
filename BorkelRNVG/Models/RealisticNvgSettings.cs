namespace BorkelRNVG.Models
{
    public sealed class RealisticNvgSettings
    {
        public float PhosphorRed { get; set; } = 0.62f;
        public float PhosphorGreen { get; set; } = 0.98f;
        public float PhosphorBlue { get; set; } = 0.92f;
        public float ManualGain { get; set; } = 1.35f;
        public float ResponseGamma { get; set; } = 0.88f;
        public float WhitePoint { get; set; } = 0.89f;
        public float BlackLevel { get; set; } = 0.002f;

        public bool AutoExposure { get; set; } = true;
        public float TargetLuminance { get; set; } = 0.18f;
        public float MinimumExposureEV { get; set; } = -2f;
        public float MaximumExposureEV { get; set; } = 5f;
        public float BrightSceneSpeed { get; set; } = 10f;
        public float DarkSceneSpeed { get; set; } = 2f;
        public float HighlightProtection { get; set; } = 0.35f;

        public bool NearDepthOfField { get; set; } = true;
        public float FullBlurDistance { get; set; } = 0.35f;
        public float SharpDistance { get; set; } = 4f;
        public float NearBlurStrength { get; set; } = 1f;
        public float NearBlurRadiusPixels { get; set; } = 16f;

        public bool OpticalHaze { get; set; }
        public float HazeBlurRadiusPixels { get; set; } = 6f;
        public float HazeCenterStrength { get; set; } = 0.1f;
        public float HazeEdgeStrength { get; set; } = 0.8f;
        public float HazeEdgeWidthPixels { get; set; } = 90f;
        public float HazeFalloff { get; set; } = 1.5f;
        public float HazeVeilStrength { get; set; } = 0.08f;
        public float ChromaticAberrationPixels { get; set; } = 1.25f;

        public bool Bloom { get; set; } = true;
        public float BloomThreshold { get; set; }
        public float BloomSoftKnee { get; set; } = 0.5f;
        public float BloomIntensity { get; set; } = 0.13f;
        public float BloomRadiusPixels { get; set; } = 22f;
        public float WideBloomRadiusPixels { get; set; } = 80f;

        public float ReadNoise { get; set; } = 0.03f;
        public float ShotNoise { get; set; } = 0.065f;
        public float CoarseNoise { get; set; } = 0.022f;
        public float FixedPatternNoise { get; set; } = 0.018f;
        public float Scintillation { get; set; } = 0.04f;
        public float ScintillationDensity { get; set; } = 1f;
        public float GrainPixelSize { get; set; } = 1f;
        public float NoiseRefreshRate { get; set; } = 60f;

        public float OpticScale { get; set; } = 1f;
        public bool FourTubeLayout { get; set; }
        public bool EdgeDistortion { get; set; } = true;
        public float DistortionStrengthPixels { get; set; } = 6f;
        public float DistortionWidthPixels { get; set; } = 60f;
        public float DistortionFalloff { get; set; } = 2f;
        public float SeamWidth { get; set; } = 0.006f;
        public float SeamOpacity { get; set; } = 0.65f;
        public bool Vignette { get; set; } = true;
        public float VignetteWidth { get; set; } = 0.12f;
        public float VignetteStrength { get; set; } = 0.68f;
        public float VignetteFalloff { get; set; } = 1.35f;

        public RealisticNvgSettings Clone()
        {
            return (RealisticNvgSettings)MemberwiseClone();
        }
    }
}
