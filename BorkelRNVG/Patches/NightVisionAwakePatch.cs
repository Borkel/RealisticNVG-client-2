using BorkelRNVG.Controllers;
using SPT.Reflection.Patching;
using BSG.CameraEffects;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using BorkelRNVG.Helpers;

namespace BorkelRNVG.Patches
{
    internal class NightVisionAwakePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NightVision), nameof(NightVision.Awake));
        }

        [PatchPrefix]
        private static void PatchPrefix(NightVision __instance)
        {
            if (__instance.GetComponent<SSAA>() == null)
                return;

            __instance.Shader = AssetHelper.nightVisionShader;
        }

        [PatchPostfix]
        private static void PatchPostfix(NightVision __instance)
        {
            if (__instance.GetComponent<SSAA>() == null)
                return;

            RealisticNightVisionRenderer renderer =
                __instance.GetComponent<RealisticNightVisionRenderer>();
            if (renderer == null)
                renderer = __instance.gameObject.AddComponent<RealisticNightVisionRenderer>();
            renderer.enabled = false;

            AmandsNvgFallbackController amandsFallback =
                __instance.GetComponent<AmandsNvgFallbackController>();
            if (amandsFallback == null)
                amandsFallback = __instance.gameObject.AddComponent<AmandsNvgFallbackController>();
            amandsFallback.SetNightVisionEnabled(__instance.On);

            if (__instance.TextureMask != null)
                __instance.TextureMask.enabled = false;
        }
    }
}
