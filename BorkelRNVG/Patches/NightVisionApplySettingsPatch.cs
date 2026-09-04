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
            if (!NvgHelper.TryGetHeadMountedNvgData(__instance, out NvgData data))
            {
                NvgHelper.DeactivateCustomPipeline(__instance);

                RealisticNightVisionRenderer existing =
                    __instance.GetComponent<RealisticNightVisionRenderer>();
                if (existing != null)
                {
                    existing.NightVisionEnabled = false;
                    existing.enabled = false;
                }

                return true;
            }

            NvgHelper.ActivateCustomPipeline(__instance);

            LensLayoutDefinition lensLayout = AssetHelper.FindLensLayout(
                data.NightVisionConfig.Values.LensLayout);
            if (lensLayout == null)
            {
                Plugin.Logger.LogError(
                    "No lens layout is available for NVG " + data.NvgItemConfig.Category);
                NvgHelper.DeactivateCustomPipeline(__instance);
                return true;
            }

            RealisticNightVisionRenderer renderer =
                __instance.GetComponent<RealisticNightVisionRenderer>();
            if (renderer == null)
                renderer = __instance.gameObject.AddComponent<RealisticNightVisionRenderer>();

            RealisticNvgSettings settings = data.NightVisionConfig.Values.Clone();
            settings.NearDepthOfField = Plugin.globalNearFocus.Value;
            settings.OpticalHaze = Plugin.globalOpticalHaze.Value;
            settings.Bloom = Plugin.globalBloom.Value;
            settings.EdgeDistortion = Plugin.globalEdgeDistortion.Value;
            settings.Vignette = Plugin.globalVignette.Value;

            renderer.ConfigureRuntime(
                AssetHelper.nightVisionShader,
                data.LensTexture,
                data.MaskTexture,
                settings,
                lensLayout,
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
