using BorkelRNVG.Helpers.Configuration;
using BorkelRNVG.Helpers.Enum;
using BorkelRNVG.Helpers;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using Systems.Effects;
using UnityEngine;

namespace BorkelRNVG.Patches
{
    public class EmitGrenadePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Effects), nameof(Effects.EmitGrenade));
        }

        [PatchPostfix]
        private static void PatchPostfix(Effects __instance, Vector3 position)
        {
            CameraClass cameraClass = Util.GetCameraClass();
            if (cameraClass == null || cameraClass.Camera == null) return;

            AutoGatingController gatingInst = AutoGatingController.Instance;
            if (gatingInst == null) return;

            string nvgId = Util.GetCurrentNvgItemId();
            if (nvgId == null) return;

            NightVisionItemConfig nvgItemConfig = NightVisionItemConfig.Get(nvgId);
            if (nvgItemConfig == null) return;

            NightVisionConfig nvgConfig = nvgItemConfig.NightVisionConfig;
            if (nvgConfig == null) return;

            EGatingType gatingType = nvgConfig.AutoGatingType.Value;
            if (gatingType == EGatingType.Off || gatingType == EGatingType.AutoGain) return;

            Player mainPlayer = Util.GetPlayer();
            Camera camera = cameraClass.Camera;

            Vector3 cameraPos = camera.transform.position;
            Vector3 dir = position - cameraPos;

            float maxShotDistance = 25f;
            float grenadeDistance = dir.magnitude;
            float grenadeDistanceMult = Mathf.Clamp01(1 - (grenadeDistance / maxShotDistance));
            bool isVisible = Util.VisibilityCheckBetweenPoints(cameraPos, position, LayerMaskClass.HighPolyWithTerrainMask);
            bool isOnScreen = Util.VisibilityCheckOnScreen(position);

            if (isVisible && isOnScreen)
            {
                float finalGatingMult = Mathf.Lerp(0, grenadeDistanceMult, grenadeDistanceMult);

                // should probably move this into Util?
                AutoGatingController.Instance?.StartCoroutine(AutoGatingController.Instance.AdjustAutoGating(0.05f, finalGatingMult, gatingInst, nvgConfig));
            }
        }
    }
}
