using System.Reflection;
using BorkelRNVG.Controllers;
using BorkelRNVG.Helpers;
using BorkelRNVG.Models;
using BSG.CameraEffects;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace BorkelRNVG.Patches
{
    internal sealed class NightVisionApplySettingsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NightVision), nameof(NightVision.ApplySettings));
        }

        [PatchPrefix]
        private static bool PatchPrefix(NightVision __instance)
        {
            string itemId = PlayerHelper.GetCurrentNvgItemId();
            NvgData data = NvgHelper.FindNvgData(itemId);
            if (data == null)
                return false;

            RealisticNightVisionRenderer renderer =
                __instance.GetComponent<RealisticNightVisionRenderer>();
            if (renderer == null)
                renderer = __instance.gameObject.AddComponent<RealisticNightVisionRenderer>();

            renderer.ConfigureRuntime(
                AssetHelper.nightVisionShader,
                data.LensTexture,
                data.MaskTexture,
                data.NightVisionConfig.Values,
                __instance.GetComponent<SSAAPropagator>(),
                Plugin.globalGain.Value,
                Plugin.globalMaskSize.Value);
            renderer.NightVisionEnabled = __instance.On;
            renderer.enabled = __instance.On;

            __instance.Mask = data.MaskTexture;
            if (__instance.TextureMask != null)
                __instance.TextureMask.enabled = false;

            return false;
        }
    }
}
