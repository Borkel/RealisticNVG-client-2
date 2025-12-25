using BorkelRNVG.Configuration;
using BorkelRNVG.Enum;
using BorkelRNVG.Helpers;
using BorkelRNVG.Controllers;
using BorkelRNVG.Models;
using Comfort.Common;
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
            string nvgId = PlayerHelper.GetCurrentNvgItemId();
            if (nvgId == null) return;
            
            NvgData nvgData = NvgHelper.GetNvgData(nvgId);
            if (nvgData == null) return;
            
            Camera camera = CameraClass.Instance.Camera;
            Vector3 cameraPos = camera.transform.position;
            Vector3 dir = position - cameraPos;

            float maxShotDistance = 25f;
            float grenadeDistance = dir.magnitude;
            float grenadeDistanceMult = Mathf.Clamp01(1f - grenadeDistance / maxShotDistance);
            bool isVisible = Util.VisibilityCheckBetweenPoints(cameraPos, position, LayerMaskClass.HighPolyWithTerrainMask);
            bool isOnScreen = Util.VisibilityCheckOnScreen(position);

            if (isVisible && isOnScreen)
            {
                float finalGatingMult = Mathf.Lerp(0, grenadeDistanceMult, grenadeDistanceMult);
                
                AutoGatingController.Instance?.StartCoroutine(AutoGatingController.Instance.AdjustAutoGating(0.05f, finalGatingMult, nvgData));
            }
        }
    }
}
