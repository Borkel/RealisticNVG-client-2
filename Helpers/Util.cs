using BorkelRNVG.Helpers.Configuration;
using BorkelRNVG.Helpers.Enum;
using BSG.CameraEffects;
using Comfort.Common;
using EFT;
using EFT.CameraControl;
using EFT.InventoryLogic;
using System;
using UnityEngine;

namespace BorkelRNVG.Helpers
{
    public class Util
    {
        private static GameWorld _gameWorld;
        private static CameraClass _fpsCamera;
        private static NightVision _nightVision;
        private static Player _mainPlayer;

        public static CameraClass GetCameraClass() => _fpsCamera;

        public static void InitializeVars()
        {
            PlayerCameraController.OnPlayerCameraControllerCreated += OnCameraCreated;
            PlayerCameraController.OnPlayerCameraControllerDestroyed += OnCameraDestroyed;
        }

        private static void OnCameraCreated(PlayerCameraController controller, Camera cam)
        {
            if (!CameraClass.Exist)
            {
                return;
            }

            _gameWorld = Singleton<GameWorld>.Instance;
            _mainPlayer = _gameWorld.MainPlayer;
            _fpsCamera = CameraClass.Instance;
            if (_fpsCamera.NightVision != null)
            {
                _nightVision = _fpsCamera.NightVision;
            }

            AutoGatingController.Create();
        }

        public static Player GetPlayer()
        {
            if (_mainPlayer == null)
            {
                _mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            }

            return _mainPlayer;
        }

        public static bool IsNvgValid()
        {
            if (_gameWorld == null || _nightVision == null || _mainPlayer == null) return false;

            if (_mainPlayer.NightVisionObserver.Component == null
                || _mainPlayer.NightVisionObserver.Component.Item == null
                || _mainPlayer.NightVisionObserver.Component.Item.TemplateId == null)
                return false;

            return true;
        }

        public static string GetCurrentNvgItemId()
        {
            if (!IsNvgValid()) return null;
        
            return _mainPlayer.NightVisionObserver.Component.Item.TemplateId;
        }

        private static void OnCameraDestroyed()
        {
            _fpsCamera = null;
            _nightVision = null;
            GameObject.Destroy(AutoGatingController.Instance.gameObject);
        }

        private static bool CheckFpsCameraExist()
        {
            if (_fpsCamera != null)
            {
                return true;
            }
            return false;
        }

        public static void ApplyNightVisionSettings(object sender, EventArgs eventArgs)
        {
            if (_nightVision == null)
            {
                if (!CheckFpsCameraExist())
                {
                    return;
                }
                _nightVision = _fpsCamera.NightVision;
            }

            _nightVision.ApplySettings();
        }

        public static void ApplyGatingSettings(object sender, EventArgs eventArgs)
        {
            NightVisionItemConfig nvgConfig = NightVisionItemConfig.Get(GetCurrentNvgItemId());

            AutoGatingController.Instance?.ApplySettings(nvgConfig.NightVisionConfig);
        }

        public static EMuzzleDeviceType GetMuzzleDeviceType(Player.FirearmController controller)
        {
            if (controller.IsSilenced) return EMuzzleDeviceType.Suppressor;

            /*Slot[] slots = controller.Item.Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].ContainedItem is FlashHiderItemClass)
                {
                    return EMuzzleDeviceType.FlashHider;
                }
            }*/

            return EMuzzleDeviceType.None;
        }

        public static bool VisibilityCheckBetweenPoints(Vector3 v1, Vector3 v2, LayerMask layer)
        {
            Vector3 dir = v2 - v1;
            Vector3 dirNormal = dir.normalized;
            float dist = dir.magnitude; // idk bruh
            bool hit = Physics.Raycast(v1, dirNormal, dist, layer);

            return !hit;
        }

        public static bool VisibilityCheckOnScreen(Vector3 pos)
        {
            Vector3 screenPos = _fpsCamera.Camera.WorldToScreenPoint(pos);
            return screenPos.z > 0 && screenPos.x > 0 && screenPos.x < Screen.width && screenPos.y > 0 && screenPos.y < Screen.height;
        }
    }
}
