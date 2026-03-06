using SPT.Reflection.Patching;
using BSG.CameraEffects;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using BorkelRNVG.Helpers;
using BorkelRNVG.Controllers;
using BorkelRNVG.Controllers.Extensions;
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
        private static bool PatchPrefix(ref NightVision __instance, ref Vector4 ___vector4_0, ref bool ___bool_1)
        {
            NvgData nvgData = ApplyModSettings(__instance);
            
            if (__instance.TextureMask == null) return true;
            
            Material lensMaterial = __instance.Material_0;
            lensMaterial.SetFloat(ShaderProperties.InvMaskSizeId, 1f / __instance.MaskSize);
            
            float invAspectValue = __instance.Mask ? __instance.Mask.height / (float)__instance.Mask.width : 1f;
            lensMaterial.SetFloat(ShaderProperties.InvAspectId, invAspectValue);

            Camera textureMaskCamera = (Camera)_materialCameraField.GetValue(__instance.TextureMask);
            float cameraAspectValue = textureMaskCamera != null ? textureMaskCamera.aspect : Screen.width / (float)Screen.height;
            lensMaterial.SetFloat(ShaderProperties.CameraAspectId, cameraAspectValue);

            float num = __instance.NoiseScale * (float)Screen.height / (float)__instance.Noise.height;
            ___vector4_0 = new Vector4(num * (float)Screen.width / (float)Screen.height, num, 0f, 0f);
            
            __instance.Material_0.SetColor(ShaderProperties.ColorId, __instance.Color_0);
            __instance.Material_0.SetFloat(ShaderProperties.NoiseIntensityId, __instance.NoiseIntensity);
            __instance.Material_0.SetVector(ShaderProperties.NoiseScaleId, ___vector4_0);
            __instance.Material_0.SetTexture(ShaderProperties.NoiseId, __instance.Noise);

            if (___bool_1)
            {
                __instance.Material_0.EnableKeyword(ShaderProperties.NightVisionNoiseKeyword);
            }
            
            if (nvgData.NightVisionConfig.AutoGatingType.Value == EGatingType.Off)
            {
                __instance.UpdateMaterialIntensity(__instance.Intensity);    
            }

            __instance.TextureMask.Mask = __instance.Mask;
            __instance.TextureMask.Size = __instance.MaskSize;
            __instance.TextureMask.ApplySettings();
            
            return false;
        }

        private static NvgData ApplyModSettings(NightVision nightVision)
        {
            Plugin.Logger.LogWarning("APPLYING MOD SETTINGS TO NIGHTVISION");
            
            string nvgId = PlayerHelper.GetCurrentNvgItemId();
            NvgData nvgData = NvgHelper.FindNvgData(nvgId);
            
            float intensity = nvgData.NightVisionConfig.Gain.Value * Plugin.globalGain.Value * (1f + 0.15f * NvgHelper.GatingLevel);
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
                autoGating.ApplySettings(nvgData);
            }

            return nvgData;
        }
    }
}
