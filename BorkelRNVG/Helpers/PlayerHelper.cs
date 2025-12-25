using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using System;

namespace BorkelRNVG.Helpers
{
    public static class PlayerHelper
    {
        public static Player LocalPlayer => Singleton<GameWorld>.Instance.MainPlayer;
        
        public static string GetCurrentNvgItemId()
        {
            string itemId = LocalPlayer?.NightVisionObserver?.Component?.Item?.StringTemplateId;

            return itemId;
        }

        public static string GetCurrentThermalItemId()
        {
            string itemId = LocalPlayer?.ThermalVisionObserver?.Component?.Item?.StringTemplateId;
            
            return itemId;
        }
    }
}
