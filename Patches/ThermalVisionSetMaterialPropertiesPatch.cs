using SPT.Reflection.Patching;
using HarmonyLib;
using System.Reflection;
using BSG.CameraEffects;
using UnityEngine;

namespace BorkelRNVG.Patches
{
    internal class ThermalVisionSetMaterialPropertiesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ThermalVision), nameof(ThermalVision.SetMaterialProperties));
        }

        [PatchPostfix]
        private static void PatchPostfix(ref ThermalVision __instance)
        {
            __instance.ThermalVisionUtilities.MaskDescription.MaskSize = Plugin.t7MaskSize.Value;
            __instance.TextureMask.Size = Plugin.t7MaskSize.Value;
            __instance.TextureMask.ApplySettings();
        }
    }
}
