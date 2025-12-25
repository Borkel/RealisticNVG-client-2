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
            return AccessTools.Method(typeof(NightVision), "Awake");
        }

        [PatchPrefix]
        private static void PatchPrefix(NightVision __instance, ref Shader ___Shader) //___Shader is the same as __instance.Shader
        {
            //replaces the masks in the class NightVision and applies visual changes
            //Plugin.UltimateBloomInstance = __instance.GetComponent<UltimateBloom>(); //to disable it when NVG turns ON
            //Plugin.BloomAndFlaresInstance = __instance.GetComponent<BloomAndFlares>(); //to disable it when NVG turns ON
            
            // gpnvg default
            __instance.AnvisMaskTexture = NvgHelper.GetNvgData(ItemIds.GPNVG).MaskTexture;
            // n-15 default
            __instance.BinocularMaskTexture = NvgHelper.GetNvgData(ItemIds.N15).MaskTexture;
            // pvs-14 default
            __instance.OldMonocularMaskTexture = NvgHelper.GetNvgData(ItemIds.PVS14).MaskTexture;
            // thermal default
            __instance.ThermalMaskTexture = NvgHelper.GetNvgData(ItemIds.T7).MaskTexture;
            __instance.Noise = AssetHelper.noiseTexture;

            // :^)
            if (__instance.Color.g > 0.9f)
            {
                ___Shader = AssetHelper.nightVisionShader;
            }
        }
    }
}
