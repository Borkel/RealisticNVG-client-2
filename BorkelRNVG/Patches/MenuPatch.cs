using SPT.Reflection.Patching;
using EFT.UI;
using EFT.UI.Screens;
using HarmonyLib;
using System.Reflection;
using System.Threading.Tasks;
using WindowsInput.Native;
using WindowsInput;
using BorkelRNVG.Helpers;

namespace BorkelRNVG.Patches
{
    internal class MenuPatch : ModulePatch
    {
        private static async Task ToggleReshadeAsync(InputSimulator inputSimulator, VirtualKeyCode key)
        {
            inputSimulator.Keyboard.KeyDown(key);
            await Task.Delay(200);
            inputSimulator.Keyboard.KeyUp(key);
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MenuTaskBar), "OnScreenChanged");
        }

        [PatchPrefix]
        private static void PatchPrefix(EEftScreenType eftScreenType)
        {
            if (!Plugin.enableReshade.Value || !Plugin.disableReshadeInMenus.Value) return;

            if (!Util.IsNvgValid()) return;

            InputSimulator inputSimulator = new InputSimulator(); // poop
            switch (eftScreenType)
            {
                case EEftScreenType.None:
                    break;
                case EEftScreenType.BattleUI:
                    if (Plugin.nvgOn)
                    {
                        Task.Run(() => ToggleReshadeAsync(inputSimulator, Plugin.nvgKey));
                    }
                    break;
                default:
                    if (Plugin.nvgOn)
                    {
                        Task.Run(() => ToggleReshadeAsync(inputSimulator, VirtualKeyCode.NUMPAD5));
                    }
                    break;
            }
        }
    }
}
