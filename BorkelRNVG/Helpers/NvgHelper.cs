using BorkelRNVG.Enum;
using BorkelRNVG.Globals;
using BorkelRNVG.Models;
using EFT;
using EFT.InventoryLogic;

namespace BorkelRNVG.Helpers
{
    public static class NvgHelper
    {
        public static NvgData GetNvgData(string itemId)
        {
            AssetHelper.NvgData.TryGetValue(itemId, out NvgData data);
            
            if (data == null)
            {
                Plugin.Logger.LogWarning($"NVG data not found for item {itemId}. Attempting to get fallback...");

                if (PlayerHelper.LocalPlayer.NightVisionObserver.Component == null) return null;
                    
                NightVisionComponent.EMask mask = PlayerHelper.LocalPlayer.NightVisionObserver.Component.Template.Mask;
                NvgData fallback = GetFallbackData(mask);
                return fallback;
            }
            
            return data;
        }

        public static ThermalData GetThermalData(string itemId)
        {
            AssetHelper.ThermalData.TryGetValue(itemId, out ThermalData data);

            if (data == null)
            {
                Plugin.Logger.LogWarning($"Thermal data not found for item {itemId}");
            }
            
            return data;
        }

        public static NvgData GetFallbackData(NightVisionComponent.EMask mask)
        {
            return mask switch
            {
                NightVisionComponent.EMask.Anvis => GetNvgData(ItemIds.GPNVG),
                NightVisionComponent.EMask.Binocular => GetNvgData(ItemIds.N15),
                NightVisionComponent.EMask.OldMonocular => GetNvgData(ItemIds.PVS14),
                NightVisionComponent.EMask.Thermal => GetNvgData(ItemIds.T7),
                _ => GetNvgData(ItemIds.N15)
            };
        }
    }
}
