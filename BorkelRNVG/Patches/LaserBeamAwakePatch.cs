using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;

namespace BorkelRNVG.Patches
{
    public class LaserInfo
    {
        public LaserBeam laserBeam;
        public float intensityFactor;
        public float maxDistance;
        public float pointSizeClose;
        public float pointSizeFar;
    }

    public class LaserBeamAwakePatch : ModulePatch
    {
        private static FieldInfo intensityField = AccessTools.Field(typeof(LaserBeam), "IntensityFactor");
        public static List<LaserInfo> ikLasers = new();

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LaserBeam), nameof(LaserBeam.method_0));
        }

        public static void UpdateAll()
        {
            for (int i = ikLasers.Count - 1; i >= 0; i--)
            {
                if (ikLasers[i] == null || ikLasers[i].laserBeam == null)
                {
                    //ikLasers.RemoveAt(i);
                    continue;
                }

                UpdateSingle(ikLasers[i]);
            }
        }

        public static void UpdateSingle(LaserInfo laserInfo)
        {
            laserInfo.laserBeam.MaxDistance = laserInfo.maxDistance * Plugin.irLaserRangeMult.Value;
            laserInfo.laserBeam.PointSizeClose = laserInfo.pointSizeClose * Plugin.irLaserPointClose.Value;
            laserInfo.laserBeam.PointSizeFar = laserInfo.pointSizeFar * Plugin.irLaserPointFar.Value;
        }

        [PatchPostfix]
        private static void PatchPostfix(LaserBeam __instance)
        {
            if (__instance.BeamMaterial.name != "LaserBeamIk") return;

            LaserInfo laserInfo = new LaserInfo()
            {
                laserBeam = __instance,
                intensityFactor = __instance.LightIntensity,
                maxDistance = __instance.MaxDistance,
                pointSizeClose = __instance.PointSizeClose,
                pointSizeFar = __instance.PointSizeFar
            };

            ikLasers.Add(laserInfo);
            UpdateSingle(laserInfo);
        }
    }
}
