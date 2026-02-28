using SPT.Reflection.Patching;
using BSG.CameraEffects;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using BorkelRNVG.Helpers;
using BorkelRNVG.Controllers;
using BorkelRNVG.Enum;
using BorkelRNVG.Models;

namespace BorkelRNVG.Patches
{
    internal class NightVisionApplySettingsPatch : ModulePatch
    {
        private static int maskId = Shader.PropertyToID("_Mask");
        private static int invMaskSizeId = Shader.PropertyToID("_InvMaskSize");
        private static int invAspectId = Shader.PropertyToID("_InvAspect");
        private static int cameraAspectId = Shader.PropertyToID("_CameraAspect");
        private static FieldInfo materialCameraField;
        
        protected override MethodBase GetTargetMethod()
        {
            materialCameraField = AccessTools.Field(typeof(TextureMask), "camera_0");
            return AccessTools.Method(typeof(NightVision), nameof(NightVision.ApplySettings));
        }

        [PatchPrefix]
        private static void PatchPrefix(ref NightVision __instance, ref TextureMask ___TextureMask, ref Texture ___Mask)
        {
            string nvgID = PlayerHelper.GetCurrentNvgItemId();
            Plugin.Log($"current nvg id: {nvgID}");
            NvgData nvgData = NvgHelper.GetNvgData(nvgID ?? "5c066e3a0db834001b7353f0");
            Material lensMaterial = __instance.Material_0;
            
            ApplyModSettings(nvgData);

            if (___TextureMask == null) return;

            lensMaterial.SetTexture(maskId, nvgData.LensTexture);
            lensMaterial.SetFloat(invMaskSizeId, 1f / __instance.MaskSize);
            
            float invAspectValue = ___Mask ? ___Mask.height / (float)___Mask.width : 1f;
            lensMaterial.SetFloat(invAspectId, invAspectValue);

            Camera textureMaskCamera = (Camera)materialCameraField.GetValue(___TextureMask);
            float cameraAspectValue = textureMaskCamera != null ? textureMaskCamera.aspect : Screen.width / (float)Screen.height;
            lensMaterial.SetFloat(cameraAspectId, cameraAspectValue);
        }

        private static void ApplyModSettings(NvgData nvgData)
        {
            if (RealisticNvgController.Instance)
            {
                RealisticNvgController.Instance.UpdateFromNvgData(nvgData);
            }
            else
            {
                Plugin.Log("RealisticNvgController not found... cant apply settings!");
            }
        }
    }
}
