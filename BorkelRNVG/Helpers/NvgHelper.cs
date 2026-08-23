using BorkelRNVG.Models;
using BorkelRNVG.Globals;
using EFT.CameraControl;
using EFT.InventoryLogic;
using NightVision = BSG.CameraEffects.NightVision;

namespace BorkelRNVG.Helpers
{
    public static class NvgHelper
    {
        public static bool IsNvgOn = false;

        public static NvgData CurrentNvgData = null;
        public static ThermalData CurrentThermalData = null;
        
        public static NvgData FindNvgData(string itemId)
        {
            itemId ??= ItemIds.N15;
            AssetHelper.NvgData.TryGetValue(itemId, out NvgData data);
            
            if (data == null)
            {
                Plugin.Log($"NVG data not found for item {itemId}. Attempting to get fallback...");

                if (PlayerHelper.LocalPlayer?.NightVisionObserver?.Component == null) return null;
                
                NightVisionComponent.EMask mask = PlayerHelper.LocalPlayer.NightVisionObserver.Component.Template.Mask;
                NvgData fallback = GetFallbackData(mask);
                
                Plugin.Log($"Loaded fallback NVG data for nvg mask: {mask.ToString()}");

                CurrentNvgData = fallback;
                
                return fallback;
            }
            
            CurrentNvgData = data;
            
            return data;
        }

        public static ThermalData FindThermalData(string itemId)
        {
            itemId ??= ItemIds.T7;
            AssetHelper.ThermalData.TryGetValue(itemId, out ThermalData data);

            if (data == null)
            {
                Plugin.Log($"Thermal data not found for item {itemId}. using default data (T7)");
            }

            CurrentThermalData = data;
            
            return data;
        }

        private static NvgData GetFallbackData(NightVisionComponent.EMask mask)
        {
            return mask switch
            {
                NightVisionComponent.EMask.Anvis => FindNvgData(ItemIds.GPNVG),
                NightVisionComponent.EMask.Binocular => FindNvgData(ItemIds.N15),
                NightVisionComponent.EMask.OldMonocular => FindNvgData(ItemIds.PVS14),
                NightVisionComponent.EMask.Thermal => FindNvgData(ItemIds.T7),
                _ => FindNvgData(ItemIds.N15)
            };
        }

        public static void ApplyNightVisionSettings()
        {
            NightVision nightVision = CameraManager.Instance?.NightVision;
            if (nightVision != null)
                nightVision.ApplySettings();
        }
    }
}
