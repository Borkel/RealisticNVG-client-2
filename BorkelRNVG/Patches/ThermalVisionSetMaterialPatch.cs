using BorkelRNVG.Controllers;
using SPT.Reflection.Patching;
using HarmonyLib;
using System.Reflection;
using BorkelRNVG.Helpers;
using BorkelRNVG.Models;
using EFT.CameraControl;

namespace BorkelRNVG.Patches
{
    internal class ThermalVisionSetMaterialPatch : ModulePatch
    {
        private const float T7DisplayHeightFraction = 870f / 1296f;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ThermalVision), nameof(ThermalVision.SetMaterialProperties));
        }

        [PatchPrefix]
        private static void PatchPrefix(ThermalVision __instance)
        {
            string itemId = PlayerHelper.GetCurrentThermalItemId();
            if (itemId == null) return;
            
            ThermalData thermalData = NvgHelper.FindThermalData(itemId);
            if (thermalData == null) return;

            MaskDescription maskDescription = __instance.ThermalVisionUtilities.MaskDescription;
            PixelationUtilities pixelationUtilities = __instance.PixelationUtilities;

            maskDescription.Mask = thermalData.MaskTexture;
            maskDescription.OldMonocularMaskTexture = thermalData.MaskTexture;
            maskDescription.ThermalMaskTexture = thermalData.MaskTexture;

            __instance.IsPixelated = thermalData.ThermalConfig.IsPixelated.Value;
            __instance.IsNoisy = thermalData.ThermalConfig.IsNoisy.Value;
            __instance.IsMotionBlurred = thermalData.ThermalConfig.IsMotionBlurred.Value;
            
            if (thermalData.ThermalConfig.IsPixelated.Value)
            {
                pixelationUtilities.Mode = 0;
                float aspect = CameraManager.Instance?.Camera?.aspect ?? (16f / 9f);
                pixelationUtilities.BlockCount =
                    thermalData.ThermalConfig.VerticalResolution.Value *
                    aspect / T7DisplayHeightFraction;
                pixelationUtilities.PixelationShader = AssetHelper.pixelationShader;
            }

            __instance.IsFpsStuck = thermalData.ThermalConfig.IsFpsStuck.Value;
            __instance.StuckFpsUtilities.MinFramerate = thermalData.ThermalConfig.MinFps.Value;
            __instance.StuckFpsUtilities.MaxFramerate = thermalData.ThermalConfig.MaxFps.Value;
        }
    }
}
