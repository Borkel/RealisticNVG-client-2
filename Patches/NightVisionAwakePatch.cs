﻿using Aki.Reflection.Patching;
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
            return AccessTools.Method(typeof(NightVision), "Awake");
        }

        [PatchPrefix]
        private static void PatchPrefix(NightVision __instance, ref Shader ___Shader)
        {
            //replaces the masks in the class NightVision and applies visual changes
            __instance.AnvisMaskTexture = Plugin.maskAnvis;
            __instance.BinocularMaskTexture = Plugin.maskBino;
            __instance.OldMonocularMaskTexture = Plugin.maskMono;
            __instance.ThermalMaskTexture = Plugin.maskMono;

            ___Shader = Plugin.nightVisionShader;
        }
    }
}