using BorkelRNVG.Configuration;
using BorkelRNVG.Enum;
using BorkelRNVG.Helpers;
using BorkelRNVG.Controllers;
using BorkelRNVG.Models;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using Systems.Effects;
using UnityEngine;

namespace BorkelRNVG.Patches
{
    public class EmitGrenadePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Effects), nameof(Effects.EmitGrenade));
        }

        [PatchPostfix]
        private static void PatchPostfix(Effects __instance, Vector3 position)
        {
            try
            {
                RealisticNvgController.Instance?.GatingController?.AdjustGatingFromFlash(position, null);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(e);
                throw;
            }
        }
    }
}
