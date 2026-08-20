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
            __instance.Shader = AssetHelper.nightVisionShader;
        }

        [PatchPostfix]
        private static void PatchPostfix(NightVision __instance)
        {
            RealisticNightVisionRenderer renderer =
                __instance.GetComponent<RealisticNightVisionRenderer>();
            if (renderer == null)
                renderer = __instance.gameObject.AddComponent<RealisticNightVisionRenderer>();
            renderer.enabled = false;

            if (__instance.TextureMask != null)
                __instance.TextureMask.enabled = false;
        }
    }
}
