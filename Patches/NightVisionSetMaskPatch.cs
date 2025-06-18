using SPT.Reflection.Patching;
using BSG.CameraEffects;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using BorkelRNVG.Helpers.Configuration;
using BorkelRNVG.Helpers;


namespace BorkelRNVG.Patches
{
    internal class NightVisionSetMaskPatch : ModulePatch
    {
        // This will patch the instance of the NightVision class
        // Thanks Fontaine, Mirni, Cj, GrooveypenguinX, Choccster, kiobu-kouhai, GrakiaXYZ, kiki, Props (sorry if i forget someone)
        
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NightVision), nameof(NightVision.SetMask));
        }

        [PatchPrefix]
        private static bool PatchPrefix(ref NightVision __instance)
        {
            string nvgID = Util.GetCurrentNvgItemId();
            if (nvgID == null) return true;

            Texture2D nvgMask = NightVisionItemConfig.Get(nvgID).MaskTexture;
            if (nvgMask == null) return true;

            __instance.Mask = nvgMask;
            return false;
        }
    }
}
