using UnityEngine;

namespace BorkelRNVG.Globals
{
    public static class ShaderProperties
    {
        // nvg shader keywords
        public static readonly string NightVisionNoiseKeyword = "NIGHT_VISION_NOISE";
        
        // nvg shader properties
        public static readonly int MaskId = Shader.PropertyToID("_Mask");
        public static readonly int InvMaskSizeId = Shader.PropertyToID("_InvMaskSize");
        public static readonly int InvAspectId = Shader.PropertyToID("_InvAspect");
        public static readonly int CameraAspectId = Shader.PropertyToID("_CameraAspect");
        public static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        public static readonly int ColorId = Shader.PropertyToID("_Color");
        public static readonly int NoiseIntensityId = Shader.PropertyToID("_NoiseIntensity");
        public static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
        public static readonly int NoiseId = Shader.PropertyToID("_Noise");
        public static readonly int NightVisionOnId = Shader.PropertyToID("_NightVisionOn");
        
        // nvg shader lens distortion properties
        public static readonly int EdgeDistortionId = Shader.PropertyToID("_EdgeDistortion");
        public static readonly int EdgeDistortionStartId = Shader.PropertyToID("_EdgeDistortionStart");
        public static readonly int LensDistortionOnId = Shader.PropertyToID("_LensDistortionOn");
        public static readonly int NearBlurOnId = Shader.PropertyToID("_NearBlurOn");
        public static readonly int NearBlurIntensityId = Shader.PropertyToID("_NearBlurIntensity");
        public static readonly int NearBlurMaxDistanceId = Shader.PropertyToID("_NearBlurMaxDistance");
        public static readonly int NearBlurKernelId = Shader.PropertyToID("_NearBlurKernel");
    }
}
