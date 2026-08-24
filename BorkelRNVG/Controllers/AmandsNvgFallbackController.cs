using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BorkelRNVG.Controllers
{
    internal sealed class AmandsNvgFallbackController : MonoBehaviour
    {
        private const string AmandsGraphicsTypeName = "AmandsGraphics.AmandsGraphicsClass";

        private static readonly ToneProfile DefaultProfile =
            new ToneProfile(new Vector3(8f, -0.1f, 8f), new Vector3(0f, 0.85f, 0f));

        private static readonly Dictionary<string, ToneProfile> Profiles =
            new Dictionary<string, ToneProfile>
            {
                ["Sandbox_Scripts"] = new ToneProfile(
                    new Vector3(25f, 0.2f, 25f), new Vector3(0f, 1.1f, 0f)),
                ["City_Scripts"] = new ToneProfile(
                    new Vector3(25f, 0.2f, 25f), new Vector3(0f, 1.1f, 0f)),
                ["Laboratory_Scripts"] = new ToneProfile(
                    new Vector3(20f, 0.4f, 20f), new Vector3(0f, 1f, 0f)),
                ["custom_Scripts"] = new ToneProfile(
                    new Vector3(20f, 0.2f, 20f), new Vector3(0f, 1f, 0f)),
                ["Factory_Rework_Day_Scripts"] = new ToneProfile(
                    new Vector3(25f, 0.6f, 25f), new Vector3(0f, 1f, 0f)),
                ["Factory_Rework_Night_Scripts"] = new ToneProfile(
                    new Vector3(25f, 0.6f, 25f), new Vector3(0f, 1f, 0f)),
                ["Lighthouse_Scripts"] = new ToneProfile(
                    new Vector3(20f, 0.2f, 20f), new Vector3(0f, 1f, 0f)),
                ["Shopping_Mall_Scripts"] = new ToneProfile(
                    new Vector3(20f, 0.2f, 18f), new Vector3(0f, 1f, 0f)),
                ["woods_Scripts"] = new ToneProfile(
                    new Vector3(20f, 0.2f, 20f), new Vector3(0f, 1f, 0f)),
                ["Reserve_Base_Scripts"] = new ToneProfile(
                    new Vector3(20f, 0.2f, 20f), new Vector3(0f, 0.85f, 0f)),
                ["shoreline_scripts"] = new ToneProfile(
                    new Vector3(20f, 0.2f, 20f), new Vector3(0f, 1f, 0f))
            };

        private bool _amandsInstalled;
        private bool _fallbackEnabled;
        private bool _fallbackApplied;
        private bool _nightVisionEnabled;
        private PrismEffects _prismEffects;
        private CC_Vintage _ccVintage;

        private Prism.Utils.TonemapType _originalTonemapType;
        private Vector3 _originalToneValues;
        private Vector3 _originalSecondaryToneValues;
        private bool _originalUseLut;
        private bool _originalVintageEnabled;

        private void Awake()
        {
            _amandsInstalled = AccessTools.TypeByName(AmandsGraphicsTypeName) != null;
            _fallbackEnabled = Plugin.enableAmandsNvgFallback?.Value ?? true;
            if (_amandsInstalled)
            {
                Plugin.Log("Amands Graphics detected; internal NVG post-processing fallback disabled.");
                enabled = false;
                return;
            }

            ResolveComponents();
            Plugin.Log("Amands Graphics not detected; internal NVG post-processing fallback enabled.");
        }

        public void SetNightVisionEnabled(bool nightVisionEnabled)
        {
            _nightVisionEnabled = nightVisionEnabled;
            if (_amandsInstalled)
                return;

            if (nightVisionEnabled && _fallbackEnabled)
                ApplyFallback();
            else
                RestoreOriginalState();
        }

        public void SetFallbackEnabled(bool fallbackEnabled)
        {
            _fallbackEnabled = fallbackEnabled;
            if (_amandsInstalled)
                return;

            if (_fallbackEnabled && _nightVisionEnabled)
                ApplyFallback();
            else
                RestoreOriginalState();
        }

        private void ApplyFallback()
        {
            if (_fallbackApplied)
                return;

            ResolveComponents();
            if (_prismEffects == null && _ccVintage == null)
                return;

            if (_prismEffects != null)
            {
                _originalTonemapType = _prismEffects.tonemapType;
                _originalToneValues = _prismEffects.toneValues;
                _originalSecondaryToneValues = _prismEffects.secondaryToneValues;
                _originalUseLut = _prismEffects.useLut;

                ToneProfile profile = GetCurrentProfile();
                _prismEffects.tonemapType = Prism.Utils.TonemapType.ACES;
                _prismEffects.toneValues = profile.Primary;
                _prismEffects.secondaryToneValues = profile.Secondary;
                _prismEffects.useLut = false;
            }

            if (_ccVintage != null)
            {
                _originalVintageEnabled = _ccVintage.enabled;
                _ccVintage.enabled = false;
            }

            _fallbackApplied = true;
        }

        private void RestoreOriginalState()
        {
            if (!_fallbackApplied)
                return;

            if (_prismEffects != null)
            {
                _prismEffects.tonemapType = _originalTonemapType;
                _prismEffects.toneValues = _originalToneValues;
                _prismEffects.secondaryToneValues = _originalSecondaryToneValues;
                _prismEffects.useLut = _originalUseLut;
            }

            if (_ccVintage != null)
                _ccVintage.enabled = _originalVintageEnabled;

            _fallbackApplied = false;
        }

        private void ResolveComponents()
        {
            if (_prismEffects == null)
                _prismEffects = GetComponent<PrismEffects>();
            if (_ccVintage == null)
                _ccVintage = GetComponent<CC_Vintage>();
        }

        private static ToneProfile GetCurrentProfile()
        {
            string scene = SceneManager.GetActiveScene().name;
            return Profiles.TryGetValue(scene, out ToneProfile profile)
                ? profile
                : DefaultProfile;
        }

        private void OnDestroy()
        {
            RestoreOriginalState();
        }

        private readonly struct ToneProfile
        {
            public readonly Vector3 Primary;
            public readonly Vector3 Secondary;

            public ToneProfile(Vector3 primary, Vector3 secondary)
            {
                Primary = primary;
                Secondary = secondary;
            }
        }
    }
}
