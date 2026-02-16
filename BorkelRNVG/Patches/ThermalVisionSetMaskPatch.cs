using BorkelRNVG.Controllers;
using SPT.Reflection.Patching;
using HarmonyLib;
using System.Reflection;
using BorkelRNVG.Helpers;
using BorkelRNVG.Models;

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
            
            RealisticNvgController.Instance.UpdateFromThermalData(thermalData);
        }
    }
}
