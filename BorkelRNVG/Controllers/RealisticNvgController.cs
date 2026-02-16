using BorkelRNVG.Enum;
using BorkelRNVG.Helpers;
using BorkelRNVG.Models;
using BSG.CameraEffects;
using UnityEngine;

namespace BorkelRNVG.Controllers
{
    public class RealisticNvgController : MonoBehaviour
    {
        public static RealisticNvgController Instance;
        
        public bool IsNvgOn;
        
        public NvgData CurrentNvgData;
        public ThermalData CurrentThermalData;
        
        public AutoGatingController GatingController;
        public NightVision NightVision;
        public ThermalVision ThermalVision;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            GatingController = gameObject.AddComponent<AutoGatingController>();

            NightVision = gameObject.GetComponent<NightVision>();
            ThermalVision = gameObject.GetComponent<ThermalVision>();
        }

        public void UpdateFromNvgData(NvgData nvgData)
        {
            CurrentNvgData = nvgData;

            if (GatingController == null)
            {
                GatingController = gameObject.AddComponent<AutoGatingController>();
            }
            
            Plugin.Log("updating nvgs from nvgdata");
            
            bool gatingEnabled = Plugin.enableAutoGating.Value && nvgData.NightVisionConfig.AutoGatingType.Value != EGatingType.Off;
            GatingController.ApplySettings(nvgData);
            GatingController.enabled = gatingEnabled;
            
            float intensity = nvgData.NightVisionConfig.Gain.Value * Plugin.globalGain.Value * (1f + 0.15f * Plugin.gatingLevel.Value);
            float noiseIntensity = 2 * nvgData.NightVisionConfig.NoiseIntensity.Value;
            float noiseSize = 2f - 2 * nvgData.NightVisionConfig.NoiseSize.Value;
            float maskSize = nvgData.NightVisionConfig.MaskSize.Value * Plugin.globalMaskSize.Value;

            NightVision.Color.a = 1f;
            NightVision.Intensity = intensity;
            NightVision.NoiseIntensity = noiseIntensity;
            NightVision.NoiseScale = noiseSize;
            NightVision.Mask = nvgData.MaskTexture;
            NightVision.MaskSize = maskSize;
            NightVision.Color.r = nvgData.NightVisionConfig.Red.Value / 255f;
            NightVision.Color.g = nvgData.NightVisionConfig.Green.Value / 255f;
            NightVision.Color.b = nvgData.NightVisionConfig.Blue.Value / 255f;
        }

        public void UpdateNvgMaterialIntensity()
        {
            NightVision.Material_0.SetFloat("_Intensity", NightVision.Intensity);
        }

        public void UpdateFromThermalData(ThermalData thermalData)
        {
            CurrentThermalData = thermalData;
            
            if (GatingController == null)
            {
                GatingController = gameObject.AddComponent<AutoGatingController>();
            }
            
            MaskDescription maskDescription = ThermalVision.ThermalVisionUtilities.MaskDescription;
            PixelationUtilities pixelationUtilities = ThermalVision.PixelationUtilities;
            StuckFPSUtilities stuckFpsUtilities = ThermalVision.StuckFpsUtilities;

            maskDescription.Mask = thermalData.MaskTexture;
            maskDescription.OldMonocularMaskTexture = thermalData.MaskTexture;
            maskDescription.ThermalMaskTexture = thermalData.MaskTexture;

            ThermalVision.IsPixelated = thermalData.ThermalConfig.IsPixelated.Value;
            ThermalVision.IsNoisy = thermalData.ThermalConfig.IsNoisy.Value;
            ThermalVision.IsMotionBlurred = thermalData.ThermalConfig.IsMotionBlurred.Value;
            
            if (thermalData.ThermalConfig.IsPixelated.Value)
            {
                pixelationUtilities.Mode = 0;
                pixelationUtilities.BlockCount = 320; //doesn't do anything really
                pixelationUtilities.PixelationMask = AssetHelper.pixelTexture;
                pixelationUtilities.PixelationShader = AssetHelper.pixelationShader;
            }

            if (thermalData.ThermalConfig.IsFpsStuck.Value)
            {
                ThermalVision.IsFpsStuck = true;
                stuckFpsUtilities.MaxFramerate = thermalData.ThermalConfig.MaxFps.Value;
                stuckFpsUtilities.MinFramerate = thermalData.ThermalConfig.MinFps.Value;
            }
        }
    }
}
