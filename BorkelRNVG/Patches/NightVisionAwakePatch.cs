using System.Reflection;
using BorkelRNVG.Controllers;
using BSG.CameraEffects;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace BorkelRNVG.Patches
{
    internal sealed class NightVisionAwakePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NightVision), nameof(NightVision.Awake));
        }

        [PatchPostfix]
        private static void PatchPostfix(NightVision __instance)
        {
            if (__instance.GetComponent<SSAA>() == null)
                return;

            // Reserve the image-effect position before SSAA's final output.
            // ApplySettings alone decides whether this camera may use NVG rendering.
            if (__instance.GetComponent<RealisticNightVisionRenderer>() != null)
                return;

            RealisticNightVisionRenderer renderer =
                __instance.gameObject.AddComponent<RealisticNightVisionRenderer>();
            renderer.NightVisionEnabled = false;
            renderer.enabled = false;
        }
    }
}
