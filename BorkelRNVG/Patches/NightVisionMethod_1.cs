using SPT.Reflection.Patching;
using BSG.CameraEffects;
using HarmonyLib;
using System.Reflection;
using BorkelRNVG.Helpers;
using BorkelRNVG.Controllers;

namespace BorkelRNVG.Patches
{
    internal class NightVisionSwitchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NightVision), nameof(NightVision.method_1));
        }

        [PatchPostfix]
        private static void PatchPostfix(NightVision __instance, bool __0)
        {
            if (!NvgHelper.UsesCustomPipeline(__instance))
            {
                AmandsNvgFallbackController inactiveFallback =
                    __instance.GetComponent<AmandsNvgFallbackController>();
                if (inactiveFallback != null)
                    inactiveFallback.SetNightVisionEnabled(false);
                return;
            }

            NvgHelper.IsNvgOn = __0;

            AmandsNvgFallbackController amandsFallback =
                __instance.GetComponent<AmandsNvgFallbackController>();
            if (amandsFallback != null)
                amandsFallback.SetNightVisionEnabled(__0);

            RealisticNightVisionRenderer renderer =
                __instance.GetComponent<RealisticNightVisionRenderer>();
            if (renderer == null)
                return;

            if (__0 && __instance.TextureMask != null)
                __instance.TextureMask.enabled = false;

            renderer.NightVisionEnabled = __0;
            renderer.enabled = __0;
        }
    }
}
