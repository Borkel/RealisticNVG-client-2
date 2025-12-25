using SPT.Reflection.Patching;
using BSG.CameraEffects;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using BorkelRNVG.Configuration;
using BorkelRNVG.Helpers;
using BorkelRNVG.Controllers;
using BorkelRNVG.Enum;
using BorkelRNVG.Models;
using EFT;

namespace BorkelRNVG.Patches
{
    internal class NightVisionApplySettingsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NightVision), nameof(NightVision.ApplySettings));
        }

        [PatchPrefix]
        private static void PatchPrefix(ref NightVision __instance, ref TextureMask ___TextureMask, ref Texture ___Mask)
        {
            ApplyModSettings(__instance);

            if (___TextureMask == null) return;

            int maskId = Shader.PropertyToID("_Mask");
            int invMaskSizeId = Shader.PropertyToID("_InvMaskSize");
            int invAspectId = Shader.PropertyToID("_InvAspect");
            int cameraAspectId = Shader.PropertyToID("_CameraAspect");

            var material = (Material)AccessTools.Property(__instance.GetType(), "Material_0").GetValue(__instance);

            string nvgID = PlayerHelper.GetCurrentNvgItemId();
            NvgData data = NvgHelper.GetNvgData(nvgID ?? "5c066e3a0db834001b7353f0");

            material.SetTexture(maskId, data.LensTexture);
            material.SetFloat(invMaskSizeId, 1f / __instance.MaskSize);

            float invAspectValue = ___Mask ? ___Mask.height / (float)___Mask.width : 1f;
            material.SetFloat(invAspectId, invAspectValue);

            Camera textureMaskCamera = (Camera)AccessTools.Field(___TextureMask.GetType(), "camera_0").GetValue(___TextureMask);
            float cameraAspectValue = textureMaskCamera != null ? textureMaskCamera.aspect : Screen.width / (float)Screen.height;
            material.SetFloat(cameraAspectId, cameraAspectValue);
        }

        private static void ApplyModSettings(NightVision nightVision)
        {
            string itemId = PlayerHelper.GetCurrentNvgItemId();
            if (itemId == null) return;
            
            NvgData nvgData = NvgHelper.GetNvgData(itemId);
            if (nvgData == null) return;
            
            EGatingType gatingType = nvgData.NightVisionConfig.AutoGatingType.Value;
            bool enableGating = gatingType != EGatingType.Off;
            
            AutoGatingController.Instance?.ApplySettings(nvgData.NightVisionConfig);
            AutoGatingController.Instance?.SetEnabled(enableGating);

            float gatingMult = AutoGatingController.Instance?.GatingMultiplier ?? 1f;
            float intensity = nvgData.NightVisionConfig.Gain.Value * Plugin.globalGain.Value * (1f + 0.15f * Plugin.gatingLevel.Value);
            float noiseIntensity = 2 * nvgData.NightVisionConfig.NoiseIntensity.Value;
            float noiseSize = 2f - 2 * nvgData.NightVisionConfig.NoiseSize.Value;
            float maskSize = nvgData.NightVisionConfig.MaskSize.Value * Plugin.globalMaskSize.Value;

            nightVision.Color.a = 1f;
            nightVision.Intensity = intensity * gatingMult;
            nightVision.NoiseIntensity = noiseIntensity;
            nightVision.NoiseScale = noiseSize;
            nightVision.Mask = nvgData.MaskTexture;
            nightVision.MaskSize = maskSize;
            nightVision.Color.r = nvgData.NightVisionConfig.Red.Value / 255f;
            nightVision.Color.g = nvgData.NightVisionConfig.Green.Value / 255f;
            nightVision.Color.b = nvgData.NightVisionConfig.Blue.Value / 255f;
        }
    }
}
