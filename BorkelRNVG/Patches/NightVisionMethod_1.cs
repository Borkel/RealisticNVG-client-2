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
        private static async Task activateReshade(InputSimulator inputSimulator, VirtualKeyCode key)
        {
            inputSimulator.Keyboard.KeyDown(key);
            await Task.Delay(200);
            inputSimulator.Keyboard.KeyUp(key);
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NightVision), "method_1");
        }

        [PatchPostfix]
        private static void PatchPostfix(bool __0) //if i use the name of the parameter it doesn't work, __0 works correctly
        {
            Plugin.nvgOn = __0;
            if (!Plugin.enableReshade.Value) return;

            if (!Util.IsNvgValid()) return;

            InputSimulator inputSimulator = new InputSimulator(); // poop
            VirtualKeyCode key = Plugin.nvgKey;
            if (__0)
                Task.Run(() => activateReshade(inputSimulator, Plugin.nvgKey));
            else if (!__0)
                Task.Run(() => activateReshade(inputSimulator, VirtualKeyCode.NUMPAD5));
        }
    }
}
