using BorkelRNVG.Helpers;
using BorkelRNVG.Configuration;
using BorkelRNVG.Enum;
using BorkelRNVG.Controllers;
using BorkelRNVG.Models;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;

namespace BorkelRNVG.Patches
{
    public class InitiateShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod(nameof(Player.FirearmController.InitiateShot));
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance, AmmoItemClass ammo, Vector3 shotPosition, Vector3 shotDirection)
        {
            try
            {
                RealisticNvgController.Instance?.GatingController?.AdjustGatingFromFlash(shotPosition, __instance);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(e);
                throw;
            }
        }
    }
}
