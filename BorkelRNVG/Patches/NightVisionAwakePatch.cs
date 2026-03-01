using BorkelRNVG.Controllers;
using SPT.Reflection.Patching;
using BSG.CameraEffects;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using BorkelRNVG.Helpers;
using BorkelRNVG.Enum;
using BorkelRNVG.Globals;
using System.IO;

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
            __instance.Noise = AssetHelper.noiseTexture;
            __instance.Shader = AssetHelper.nightVisionShader;
        }
    }
}
