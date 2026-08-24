using EFT.Visual;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BorkelRNVG.Patches
{
    public class LightInfo
    {
        public IkLight ikLight;
        public Light light;
        public float intensity;
        public float range;
    }

    public class IkLightAwakePatch : ModulePatch
    {
        private static FieldInfo _intensityField = AccessTools.Field(typeof(IkLight), "float_0");
        private static List<LightInfo> _ikLights = new();

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(IkLight), nameof(IkLight.Awake));
        }

        public static void UpdateAll()
        {
            for (int i = _ikLights.Count - 1; i >= 0; i--)
            {
                if (_ikLights[i].ikLight == null || _ikLights[i].light == null)
                {
                    //_ikLights.RemoveAt(i);
                    continue;
                }

                UpdateSingle(_ikLights[i]);
            }
        }

        public static void UpdateSingle(LightInfo lightInfo)
        {
            if (lightInfo.light == null)
            {
                //_ikLights.RemoveAt(_ikLights.IndexOf(lightInfo));
                return;
            }

            _intensityField.SetValue(lightInfo.ikLight, lightInfo.intensity * Plugin.irFlashlightBrightnessMult.Value);
            lightInfo.light.range = lightInfo.range * Plugin.irFlashlightRangeMult.Value;
        }

        [PatchPostfix]
        private static void PatchPostfix(IkLight __instance)
        {
            Light spotLight = __instance.Light;
            float intensity = (float)_intensityField.GetValue(__instance);

            if (__instance == null || spotLight == null) return;

            float range = spotLight.range;

            LightInfo lightInfo = new LightInfo
            {
                ikLight = __instance,
                light = spotLight,
                intensity = intensity,
                range = range
            };

            _ikLights.Add(lightInfo);
            UpdateSingle(lightInfo);
        }
    }
}
