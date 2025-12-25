using BorkelRNVG.Controllers;
using BorkelRNVG.Helpers;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace BorkelRNVG.Patches
{
    public class GameStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        private static void PatchPostfix(GameWorld __instance)
        {
            AssetHelper.LoadAudioClips();
            AutoGatingController.Create();
        }
    }
}
