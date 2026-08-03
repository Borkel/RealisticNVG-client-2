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
            return AccessTools.Method(typeof(NightVision), nameof(NightVision.Switch));
        }

        [PatchPostfix]
        private static void PatchPostfix(bool on)
        {
            NvgHelper.IsNvgOn = on;
        }
    }
}
