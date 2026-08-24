using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace BorkelRNVG.Patches
{
    public class LaserBeamLateUpdatePatch : ModulePatch
    {
        private static FieldInfo intensityField = AccessTools.Field(typeof(LaserBeam), "IntensityFactor");
        private static FieldInfo beamBlockField = AccessTools.Field(typeof(LaserBeam), "_beamBlock");
        private static readonly int colorId = Shader.PropertyToID("_Color");

        private struct VisibleLaserState
        {
            public bool modified;
            public float vanillaIntensity;
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LaserBeam), nameof(LaserBeam.LateUpdate));
        }

        [PatchPrefix]
        private static void PatchPrefix(LaserBeam __instance, out VisibleLaserState __state)
        {
            __state = default;

            if (__instance == null || __instance.BeamMaterial == null)
                return;

            bool isIrLaser = __instance.BeamMaterial.name == "LaserBeamIk";
            bool nvgOn = Helpers.NvgHelper.IsNvgOn;

            if (__instance.BeamMaterial.HasProperty(colorId))
            {
                MaterialPropertyBlock beamBlock =
                    (MaterialPropertyBlock)beamBlockField.GetValue(__instance);
                if (beamBlock != null)
                {
                    float beamMultiplier = isIrLaser
                        ? Plugin.irLaserBrightnessMult.Value
                        : nvgOn
                            ? Plugin.visibleLaserBrightnessWithNvgs.Value
                            : 1f;

                    beamBlock.SetColor(
                        colorId,
                        __instance.BeamMaterial.GetColor(colorId) * beamMultiplier);
                }
            }

            if (!isIrLaser && nvgOn)
            {
                __state.modified = true;
                __state.vanillaIntensity =
                    (float)intensityField.GetValue(__instance);
                intensityField.SetValue(
                    __instance,
                    __state.vanillaIntensity *
                    Plugin.visibleLaserBrightnessWithNvgs.Value);
            }
        }

        [PatchPostfix]
        private static void PatchPostfix(
            LaserBeam __instance,
            VisibleLaserState __state)
        {
            if (__state.modified && __instance != null)
                intensityField.SetValue(__instance, __state.vanillaIntensity);

            if (__instance == null || __instance.BeamMaterial == null ||
                __instance.BeamMaterial.name != "LaserBeamIk") return;

            Vector3 position = __instance.transform.position;
            Vector3 forward = __instance.transform.forward;
            RaycastHit hitInfo;
            bool hit = Physics.Raycast(position + forward * __instance.RayStart, forward, out hitInfo, __instance.MaxDistance, __instance.Mask);
            float lerp = 1 - Mathf.Clamp01(hitInfo.distance / __instance.MaxDistance);

            if (hit)
            {
                intensityField.SetValue(__instance, Mathf.Lerp(0.001f, 0.01f, lerp) * Plugin.irLaserBrightnessMult.Value);
            }
            else
            {
                intensityField.SetValue(__instance, 0f);
            }
        }
    }
}
