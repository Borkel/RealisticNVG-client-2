using BorkelRNVG.Controllers;
using SPT.Reflection.Patching;
using BSG.CameraEffects;
using HarmonyLib;
using System.Reflection;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;
using BorkelRNVG.Helpers;

namespace BorkelRNVG.Patches
{
    internal class NightVisionMethod_1 : ModulePatch //method_1 gets called when NVGs turn off or on, tells the reshade to activate
    {
        private static async Task ActivateReshade(InputSimulator inputSimulator, VirtualKeyCode key)
        {
            inputSimulator.Keyboard.KeyDown(key);
            await Task.Delay(200);
            inputSimulator.Keyboard.KeyUp(key);
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NightVision), nameof(NightVision.method_1));
        }

        [PatchPostfix]
        private static void PatchPostfix(bool __0) //if i use the name of the parameter it doesn't work, __0 works correctly
        {
            Plugin.Log($"toggling nvg overlay: {__0}");

            NvgHelper.IsNvgOn = __0;
        }
    }
}
