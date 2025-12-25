using SPT.Reflection.Patching;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using LightStruct = FirearmLightStateStruct; //public static void Serialize(GInterface63 stream, ref GStruct155 tacticalComboStatus)
using static EFT.Player;
using System.Collections;

namespace BorkelRNVG.Patches
{
    internal class SprintPatch : ModulePatch
    {
        //private static async Task ToggleLaserWithDelay(FirearmController fc, LightComponent light, bool newState, int delay)
        private static IEnumerator ToggleLaserWithDelay(FirearmController firearmController, LightComponent light, bool newState, float delay)
        {
            //await Task.Delay(delay);
            yield return new WaitForSeconds(delay);

            firearmController.SetLightsState(new LightStruct[]
            {
                new LightStruct
                {
                    Id = light.Item.Id,
                    IsActive = newState,
                    LightMode = light.SelectedMode
                }
            }, false);
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.LateUpdate));
        }

        [PatchPostfix]
        private static void PatchPostfix(ref Player __instance)
        {
            if (!__instance.IsYourPlayer ||
                __instance.CurrentManagedState == null ||
                __instance.CurrentManagedState.Name == EPlayerState.Jump ||
                !Plugin.enableSprintPatch.Value ||
                __instance.HandsController == null)
                return;

            Plugin.isSprinting = __instance.IsSprintEnabled;
            FirearmController firearmController = __instance.HandsController as FirearmController;
            if (firearmController == null) return;

            if (Plugin.isSprinting != Plugin.wasSprinting) //if the player goes from sprinting to not sprinting, or from not sprinting to sprinting
            {
                foreach (Mod modification in firearmController.Item.Mods)
                {
                    LightComponent lightComponent;
                    if (modification.TryGetItemComponent<LightComponent>(out lightComponent))
                    {
                        if (!Plugin.LightDictionary.ContainsKey(modification.Id))
                            Plugin.LightDictionary.Add(modification.Id, false);

                        bool state = false;

                        if (Plugin.isSprinting == false && !lightComponent.IsActive && Plugin.LightDictionary[modification.Id])
                        {
                            state = true;
                            Plugin.LightDictionary[modification.Id] = false;
                            //Task.Run(() => ToggleLaserWithDelay(fc, light, state, 300));
                            firearmController.StartCoroutine(ToggleLaserWithDelay(firearmController, lightComponent, state, 0.3f)); //delay of 300ms when turning on
                            //delay of 300ms when turning on
                        }
                        else if (Plugin.isSprinting == true && lightComponent.IsActive)
                        {
                            state = false;
                            Plugin.LightDictionary[modification.Id] = true;
                            //Task.Run(() => ToggleLaserWithDelay(fc, light, state, 100));
                            firearmController.StartCoroutine(ToggleLaserWithDelay(firearmController, lightComponent, state, 0.1f));
                        }
                    }
                }
            }
            Plugin.wasSprinting = Plugin.isSprinting;
        }
    }
}
