using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace BorkelRNVG.Patches
{
    public class LaserBeamLateUpdatePatch : ModulePatch
    {
        private static FieldInfo intensityField = AccessTools.Field(typeof(LaserBeam), "IntensityFactor");

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LaserBeam), nameof(LaserBeam.LateUpdate));
        }

        [PatchPostfix]
        private static void PatchPostfix(LaserBeam __instance)
        {
            if (__instance.BeamMaterial.name != "LaserBeamIk" || __instance == null) return;

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
