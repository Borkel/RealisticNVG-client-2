using SPT.Reflection.Patching;
using HarmonyLib;
using System.Reflection;
using BorkelRNVG.Helpers;
using BorkelRNVG.Enum;
using BorkelRNVG.Models;
using Comfort.Common;
using EFT;

namespace BorkelRNVG.Patches
{
    internal class ThermalVisionSetMaskPatch : ModulePatch
    {
        // This will patch the instance of the ThermalVision class to edit the T-7

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ThermalVision), nameof(ThermalVision.SetMask));
        }

        [PatchPrefix]
        private static void PatchPrefix(ref ThermalVision __instance)
        {
            string itemId = PlayerHelper.GetCurrentThermalItemId();
            if (itemId == null) return;

            ThermalData thermalData = NvgHelper.GetThermalData(itemId);
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
                __instance.PixelationUtilities = new PixelationUtilities();
                
                pixelationUtilities.Mode = 0;
                pixelationUtilities.BlockCount = 320; //doesn't do anything really
                pixelationUtilities.PixelationMask = AssetHelper.pixelTexture;
                pixelationUtilities.PixelationShader = AssetHelper.pixelationShader;
            }

            if (thermalData.ThermalConfig.IsFpsStuck.Value)
            {
                __instance.IsFpsStuck = true;
                __instance.StuckFpsUtilities = new StuckFPSUtilities()
                {
                    MinFramerate = thermalData.ThermalConfig.MinFps.Value,
                    MaxFramerate = thermalData.ThermalConfig.MaxFps.Value
                };
            }
        }
    }
}
