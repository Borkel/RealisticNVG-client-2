using SPT.Reflection.Patching;
using BSG.CameraEffects;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using BorkelRNVG.Helpers;
using BorkelRNVG.Controllers;
using BorkelRNVG.Enum;
using BorkelRNVG.Globals;
using BorkelRNVG.Models;

namespace BorkelRNVG.Patches
{
    internal class NightVisionApplySettingsPatch : ModulePatch
    {
        private static FieldInfo _materialCameraField;
        
        protected override MethodBase GetTargetMethod()
        {
            _materialCameraField = AccessTools.Field(typeof(TextureMask), "camera_0");
            return AccessTools.Method(typeof(NightVision), nameof(NightVision.ApplySettings));
        }

        [PatchPrefix]
        private static void PatchPrefix(ref NightVision __instance, ref TextureMask ___TextureMask, ref Texture ___Mask)
        {
            ApplyModSettings(__instance);
            
            if (___TextureMask == null) return;
            
            Material lensMaterial = __instance.Material_0;
            lensMaterial.SetFloat(ShaderProperties.InvMaskSizeId, 1f / __instance.MaskSize);
            
            float invAspectValue = ___Mask ? ___Mask.height / (float)___Mask.width : 1f;
            lensMaterial.SetFloat(ShaderProperties.InvAspectId, invAspectValue);

            Camera textureMaskCamera = (Camera)_materialCameraField.GetValue(___TextureMask);
            float cameraAspectValue = textureMaskCamera != null ? textureMaskCamera.aspect : Screen.width / (float)Screen.height;
            lensMaterial.SetFloat(ShaderProperties.CameraAspectId, cameraAspectValue);
        }

        private static void ApplyModSettings(NightVision nightVision)
        {
            Plugin.Logger.LogWarning("APPLYING MOD SETTINGS TO NIGHTVISION");
            
            string nvgId = PlayerHelper.GetCurrentNvgItemId();
            NvgData nvgData = NvgHelper.FindNvgData(nvgId);
            
            float intensity = nvgData.NightVisionConfig.Gain.Value * Plugin.globalGain.Value * (1f + 0.15f * Plugin.gatingLevel.Value);
            float noiseIntensity = 2 * nvgData.NightVisionConfig.NoiseIntensity.Value;
            float noiseSize = 2f - 2 * nvgData.NightVisionConfig.NoiseSize.Value;
            float maskSize = nvgData.NightVisionConfig.MaskSize.Value * Plugin.globalMaskSize.Value;

            // update nvg properties
            nightVision.Color.a = 1f;
            nightVision.Intensity = intensity;
            nightVision.NoiseIntensity = noiseIntensity;
            nightVision.NoiseScale = noiseSize;
            nightVision.Mask = nvgData.MaskTexture;
            nightVision.MaskSize = maskSize;
            nightVision.Color.r = nvgData.NightVisionConfig.Red.Value / 255f;
            nightVision.Color.g = nvgData.NightVisionConfig.Green.Value / 255f;
            nightVision.Color.b = nvgData.NightVisionConfig.Blue.Value / 255f;
            
            // update nvg lens texture
            Material lensMaterial = nightVision.Material_0;
            lensMaterial.SetTexture(ShaderProperties.MaskId, nvgData.LensTexture);
            
            // update lens distortion from nvgData
            lensMaterial.SetFloat(ShaderProperties.EdgeDistortionId, nvgData.NightVisionConfig.EdgeDistortion.Value);
            lensMaterial.SetFloat(ShaderProperties.EdgeDistortionStartId, nvgData.NightVisionConfig.EdgeDistortionStart.Value);
            
            // update lens distortion from global config
            lensMaterial.SetFloat(ShaderProperties.LensDistortionOnId, Plugin.globalLensDistortion.Value ? 1f : 0f);
            lensMaterial.SetFloat(ShaderProperties.NearBlurOnId, Plugin.globalNearBlur.Value ? 1f : 0f);
            lensMaterial.SetFloat(ShaderProperties.NearBlurIntensityId, Plugin.globalBlurIntensity.Value);
            lensMaterial.SetFloat(ShaderProperties.NearBlurMaxDistanceId, Plugin.globalBlurDistance.Value);
            lensMaterial.SetFloat(ShaderProperties.NearBlurKernelId, Plugin.globalBlurQuality.Value);

            // apply autogating settings
            AutoGatingController autoGating = AutoGatingController.Instance;
            if (autoGating != null)
            {
                bool enableGating = NvgHelper.ShouldEnableGating(nvgData);
                
                autoGating.enabled = enableGating;

                // only apply settings when enabling night vision
                if (!NvgHelper.IsNvgOn)
                {
                    autoGating.ApplySettings(nvgData);
                }
            }
        }
    }
}
