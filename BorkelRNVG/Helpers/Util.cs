using BorkelRNVG.Configuration;
using BorkelRNVG.Enum;
using BorkelRNVG.Controllers;
using BorkelRNVG.Models;
using BSG.CameraEffects;
using Comfort.Common;
using EFT;
using EFT.CameraControl;
using EFT.InventoryLogic;
using System;
using UnityEngine;

namespace BorkelRNVG.Helpers
{
    public static class Util
    {
        public static bool IsNvgValid()
        {
            return Singleton<GameWorld>.Instance.MainPlayer.NightVisionObserver.Component?.Item?.StringTemplateId != null;
        }

        public static void ApplyNightVisionSettings()
        {
            NightVision nightVision = CameraClass.Instance.NightVision;

            nightVision.ApplySettings();
        }

        public static void ApplyGatingSettings()
        {
            string itemId = PlayerHelper.GetCurrentNvgItemId();
            if (itemId == null) return;

            NvgData nvgData = NvgHelper.GetNvgData(itemId);
            if (nvgData == null) return;

            AutoGatingController.Instance?.ApplySettings(nvgData.NightVisionConfig);
        }

        public static EMuzzleDeviceType GetMuzzleDeviceType(Player.FirearmController controller)
        {
            if (controller.IsSilenced) return EMuzzleDeviceType.Suppressor;

            Slot[] slots = controller.Weapon.Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].ContainedItem is FlashHiderItemClass)
                {
                    return EMuzzleDeviceType.FlashHider;
                }
            }

            return EMuzzleDeviceType.None;
        }

        public static bool VisibilityCheckBetweenPoints(Vector3 v1, Vector3 v2, LayerMask layer)
        {
            Vector3 dir = v2 - v1;
            Vector3 dirNormal = dir.normalized;
            float dist = dir.magnitude;
            bool hit = Physics.Raycast(v1, dirNormal, dist, layer);

            return !hit;
        }

        public static bool VisibilityCheckOnScreen(Vector3 pos)
        {
            Vector3 screenPos = CameraClass.Instance.Camera.WorldToScreenPoint(pos);
            return screenPos.z > 0 && screenPos.x > 0 && screenPos.x < Screen.width && screenPos.y > 0 && screenPos.y < Screen.height;
        }
    }
}
