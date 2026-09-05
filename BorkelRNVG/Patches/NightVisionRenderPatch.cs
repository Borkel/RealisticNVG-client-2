using System.Reflection;
using BorkelRNVG.Controllers;
using BSG.CameraEffects;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine.Rendering;

namespace BorkelRNVG.Patches
{
    internal sealed class NightVisionRenderPatch : ModulePatch
    {
        private static FieldInfo commandBufferField;
        private static FieldInfo ssaaPropagatorField;

        protected override MethodBase GetTargetMethod()
        {
            commandBufferField = AccessTools.Field(typeof(NightVision), "_nightVisionCB");
            ssaaPropagatorField = AccessTools.Field(typeof(NightVision), "_ssaaPropagator");
            return AccessTools.Method(typeof(NightVision), nameof(NightVision.OnPreCull));
        }

        [PatchPostfix]
        private static void PatchPostfix(NightVision __instance)
        {
            if (!Helpers.NvgHelper.UsesCustomPipeline(__instance) || !__instance.On)
                return;

            CommandBuffer commandBuffer =
                (CommandBuffer)commandBufferField.GetValue(__instance);
            commandBuffer?.Clear();

            SSAAPropagator propagator =
                (SSAAPropagator)ssaaPropagatorField.GetValue(__instance);
            propagator?.SetNightVisionMaterial(null);

            RealisticNightVisionRenderer renderer =
                __instance.GetComponent<RealisticNightVisionRenderer>();
            if (renderer == null)
                return;

            if (__instance.TextureMask != null)
                __instance.TextureMask.enabled = false;

            renderer.NightVisionEnabled = __instance.On;
            renderer.enabled = __instance.On;
        }
    }
}
