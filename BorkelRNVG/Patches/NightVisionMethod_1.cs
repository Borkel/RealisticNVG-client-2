using SPT.Reflection.Patching;
using BSG.CameraEffects;
using HarmonyLib;
using System.Reflection;
using BorkelRNVG.Helpers;

namespace BorkelRNVG.Patches
{
    internal class NightVisionSwitchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NightVision), nameof(NightVision.method_1));
        }

        [PatchPostfix]
        private static void PatchPostfix(bool __0)
        {
            NvgHelper.IsNvgOn = __0;
        }
    }
}
