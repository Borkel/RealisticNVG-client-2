using BorkelRNVG.Controllers;
using SPT.Reflection.Patching;
using HarmonyLib;
using System.Reflection;
using BorkelRNVG.Helpers;
using BorkelRNVG.Models;

namespace BorkelRNVG.Patches
{
    internal class ThermalVisionSetMaterialPatch : ModulePatch
    {
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
                pixelationUtilities.BlockCount = 320; //doesn't do anything really
                pixelationUtilities.PixelationMask = AssetHelper.pixelTexture;
                pixelationUtilities.PixelationShader = AssetHelper.pixelationShader;
            }

            __instance.IsFpsStuck = thermalData.ThermalConfig.IsFpsStuck.Value;
            __instance.StuckFpsUtilities.MinFramerate = thermalData.ThermalConfig.MinFps.Value;
            __instance.StuckFpsUtilities.MaxFramerate = thermalData.ThermalConfig.MaxFps.Value;
        }
    }
}
