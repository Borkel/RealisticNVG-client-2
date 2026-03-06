using BorkelRNVG.Globals;
using BorkelRNVG.Helpers;
using BorkelRNVG.Models;
using BSG.CameraEffects;
using UnityEngine;

namespace BorkelRNVG.Controllers.Extensions
{
    public static class NightVisionExtensions
    {
        public static void UpdateIntensity(this NightVision nightVision)
        {
            float intensity = NvgHelper.CurrentNvgData.NightVisionConfig.Gain.Value * Plugin.globalGain.Value * (1f + 0.15f * NvgHelper.GatingLevel);
            nightVision.Intensity = intensity;
            nightVision.UpdateMaterialIntensity(intensity);
        }

        public static void MultiplyIntensity(this NightVision nightVision, params float[] multipliers)
        {
            float intensity = NvgHelper.CurrentNvgData.NightVisionConfig.Gain.Value * Plugin.globalGain.Value * (1f + 0.15f * NvgHelper.GatingLevel);
            float minIntensity = Plugin.clampMinGating.Value ? NvgHelper.CurrentNvgData.NightVisionConfig.MinBrightness.Value : 0f;

            foreach (float mult in multipliers)
            {
                intensity *= mult;
            }

            intensity = Mathf.Max(intensity, minIntensity);
            
            nightVision.UpdateMaterialIntensity(intensity);
        }
        
        public static void UpdateMaterialIntensity(this NightVision nightVision, float intensity)
        {
            nightVision.Material_0.SetFloat(ShaderProperties.IntensityId, intensity);
        }
    }
}
