using SPT.Reflection.Patching;
using BSG.CameraEffects;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using BorkelRNVG.Helpers.Configuration;
using BorkelRNVG.Helpers;

namespace BorkelRNVG.Patches
{
    internal class NightVisionApplySettingsPatch : ModulePatch
    {
        public static List<NightVisionItemConfig> nightVisionConfigs = new List<NightVisionItemConfig>();
        private static NightVision _nightVision;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NightVision), nameof(NightVision.ApplySettings));
        }

        [PatchPrefix]
        private static void PatchPrefix(ref NightVision __instance, ref TextureMask ___TextureMask, ref Texture ___Mask)
        {
            if (_nightVision == null)
            {
                _nightVision = __instance;
            }

            ApplyModSettings(__instance);

            if (___TextureMask == null) return;

            int maskId = Shader.PropertyToID("_Mask");
            int invMaskSizeId = Shader.PropertyToID("_InvMaskSize");
            int invAspectId = Shader.PropertyToID("_InvAspect");
            int cameraAspectId = Shader.PropertyToID("_CameraAspect");

            var material = (Material)AccessTools.Property(__instance.GetType(), "Material_0").GetValue(__instance);

            string nvgID = Util.GetCurrentNvgItemId();

            if (nvgID != null)
            {
                NightVisionItemConfig nvgItemConfig = NightVisionItemConfig.Get(nvgID);
                if (nvgItemConfig != null)
                {
                    material.SetFloat("_EdgeDistortion", (nvgItemConfig.EdgeDistortion/10));
                    material.SetFloat("_EdgeDistortionStart", (nvgItemConfig.EdgeDistortionStart));
                }
                Texture lens = nvgItemConfig.LensTexture;

                material.SetTexture(maskId, lens);
            }
            else
            {
                Texture lens = AssetHelper.MaskToLens(___Mask);
                if (lens != null)
                {
                    material.SetTexture(maskId, lens);
                }
            }

            material.SetFloat(invMaskSizeId, 1f / __instance.MaskSize);

            float invAspectValue = ___Mask != null
                ? ___Mask.height / (float)___Mask.width
                : 1f;
            material.SetFloat(invAspectId, invAspectValue);

            var textureMaskCamera = (Camera)AccessTools.Field(___TextureMask.GetType(), "camera_0").GetValue(___TextureMask);
            float cameraAspectValue = textureMaskCamera != null
                ? textureMaskCamera.aspect
                : Screen.width / (float)Screen.height;
            material.SetFloat(cameraAspectId, cameraAspectValue);
        }

        public static void ApplyModSettings(NightVision nightVision)
        {
            string nvgID = Util.GetCurrentNvgItemId();
            if (nvgID == null) return;

            NightVisionItemConfig nvgItemConfig = NightVisionItemConfig.Get(nvgID);
            NightVisionConfig nvgConfig = nvgItemConfig.NightVisionConfig;

            if (nvgItemConfig != null)
            {
                nvgItemConfig.Update();

                // grab the values from the (now updated) night vision item config
                nightVision.Color.a = 1;
                nightVision.Intensity = nvgItemConfig.Intensity;
                nightVision.NoiseIntensity = nvgItemConfig.NoiseIntensity;
                nightVision.NoiseScale = nvgItemConfig.NoiseScale;
                nightVision.Mask = nvgItemConfig.MaskTexture;
                nightVision.MaskSize = nvgItemConfig.MaskSize;
                nightVision.Color.r = nvgItemConfig.R;
                nightVision.Color.g = nvgItemConfig.G;
                nightVision.Color.b = nvgItemConfig.B;
                Plugin.nvgKey = nvgItemConfig.Key;
            }

            AutoGatingController.Instance?.ApplySettings(nvgConfig);
        }
    }
}