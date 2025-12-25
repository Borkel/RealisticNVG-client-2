using BorkelRNVG.Helpers;
using BorkelRNVG.Configuration;
using BorkelRNVG.Enum;
using BorkelRNVG.Controllers;
using BorkelRNVG.Models;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace BorkelRNVG.Patches
{
    public class InitiateShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod(nameof(Player.FirearmController.InitiateShot));
        }

        private static float EaseOut(float val)
        {
            return 1 - Mathf.Pow(1 - val, 3);
        }

        private static float ClampDot90Deg(float dot)
        {
            return Mathf.Max(0, dot);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance, AmmoItemClass ammo, Vector3 shotPosition, Vector3 shotDirection)
        {
            string itemId = PlayerHelper.GetCurrentNvgItemId();
            if (itemId == null) return;

            NvgData nvgData = NvgHelper.GetNvgData(itemId);
            if (nvgData == null) return;
            
            EGatingType gatingType = nvgData.NightVisionConfig.AutoGatingType.Value;
            if (gatingType != EGatingType.AutoGating) return;

            EMuzzleDeviceType deviceType = Util.GetMuzzleDeviceType(__instance);

            Player mainPlayer = PlayerHelper.LocalPlayer;
            Player firearmOwner = __instance.GetComponentInParent<Player>();
            Camera camera = CameraClass.Instance.Camera;

            float gatingLerp;
            switch (deviceType)
            {
                case EMuzzleDeviceType.None:
                    gatingLerp = 0.15f;
                    break;
                case EMuzzleDeviceType.Suppressor:
                    gatingLerp = 1.0f;
                    break;
                case EMuzzleDeviceType.FlashHider:
                    gatingLerp = 0.3f;
                    break;
                default:
                    gatingLerp = 1.0f;
                    break;
            }

            if (firearmOwner != mainPlayer)
            {
                Vector3 cameraPos = camera.transform.position;
                Vector3 dir = shotPosition - cameraPos;

                float maxShotDistance = 15f;
                float shotDistance = dir.magnitude;
                float shotDistanceMult = Mathf.Clamp01(1 - (shotDistance / maxShotDistance));
                bool isVisible = Util.VisibilityCheckBetweenPoints(cameraPos, shotPosition, LayerMaskClass.HighPolyWithTerrainMask);
                bool isOnScreen = Util.VisibilityCheckOnScreen(shotPosition);

                if (isVisible && isOnScreen)
                {
                    float finalGatingMult = Mathf.Lerp(0, shotDistanceMult, gatingLerp);
                    AutoGatingController.Instance?.StartCoroutine(AutoGatingController.Instance.AdjustAutoGating(0.05f, finalGatingMult, nvgData));
                }
            }
            else
            {
                AutoGatingController.Instance?.StartCoroutine(AutoGatingController.Instance.AdjustAutoGating(0.05f, gatingLerp, nvgData));
            }
        }
    }
}
