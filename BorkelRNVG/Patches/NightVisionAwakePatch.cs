using BorkelRNVG.Controllers;
using SPT.Reflection.Patching;
using BSG.CameraEffects;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace BorkelRNVG.Patches
{
    internal class NightVisionAwakePatch : ModulePatch
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

            RealisticNightVisionRenderer renderer =
                __instance.GetComponent<RealisticNightVisionRenderer>();
            if (renderer != null)
                return;

            renderer = __instance.gameObject.AddComponent<RealisticNightVisionRenderer>();
            renderer.NightVisionEnabled = false;
            renderer.enabled = false;
        }
    }
}
