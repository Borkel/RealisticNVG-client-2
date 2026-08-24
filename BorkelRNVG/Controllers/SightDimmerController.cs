using System.Collections.Generic;
using System.Reflection;
using BorkelRNVG.Helpers;
using EFT;
using EFT.CameraControl;
using UnityEngine;

namespace BorkelRNVG.Controllers
{
    internal sealed class SightDimmerController
    {
        private const float UpdateInterval = 0.15f;

        private static readonly int HdrId = Shader.PropertyToID("_HDR");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly FieldInfo ReticleMaterialField = typeof(OpticRetrice)
            .GetField("material_0", BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly Dictionary<int, float> _collimatorBaseHdr = new Dictionary<int, float>();
        private readonly Dictionary<int, Color> _scopeBaseColor = new Dictionary<int, Color>();
        private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();

        private float _elapsed;

        public void Tick(bool enabled, float collimatorMultiplier, float scopeMultiplier)
        {
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < UpdateInterval)
                return;

            _elapsed = 0f;
            Apply(enabled, collimatorMultiplier, scopeMultiplier);
        }

        public void ApplyImmediately(bool enabled, float collimatorMultiplier, float scopeMultiplier)
        {
            _elapsed = 0f;
            Apply(enabled, collimatorMultiplier, scopeMultiplier);
        }

        private void Apply(bool enabled, float collimatorMultiplier, float scopeMultiplier)
        {
            Transform weaponRoot = PlayerHelper.LocalPlayer?.PlayerBones?.WeaponRoot?.Original;
            if (weaponRoot == null)
                return;

            bool dim = enabled && NvgHelper.IsNvgOn;
            ApplyToCollimators(weaponRoot, dim ? collimatorMultiplier : 1f);
            ApplyToScopeReticle(dim ? scopeMultiplier : 1f);
        }

        private void ApplyToCollimators(Transform weaponRoot, float multiplier)
        {
            CollimatorSight[] sights = weaponRoot.GetComponentsInChildren<CollimatorSight>(true);
            foreach (CollimatorSight sight in sights)
            {
                Renderer renderer = sight.CollimatorMeshRenderer;
                if (renderer == null)
                    continue;

                Material material = renderer.sharedMaterial;
                int rendererId = renderer.GetInstanceID();
                if (!_collimatorBaseHdr.TryGetValue(rendererId, out float baseHdr))
                {
                    baseHdr = material != null && material.HasProperty(HdrId)
                        ? material.GetFloat(HdrId)
                        : 3f;
                    _collimatorBaseHdr[rendererId] = baseHdr;
                }

                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(HdrId, baseHdr * Mathf.Clamp01(multiplier));
                renderer.SetPropertyBlock(_propertyBlock);
                _propertyBlock.Clear();
            }
        }

        private void ApplyToScopeReticle(float multiplier)
        {
            OpticRetrice reticle = CameraClass.Instance?.OpticCameraManager?.OpticRetrice;
            if (reticle == null || ReticleMaterialField == null)
                return;

            Material material = ReticleMaterialField.GetValue(reticle) as Material;
            if (material == null || !material.HasProperty(ColorId))
                return;

            int materialId = material.GetInstanceID();
            if (!_scopeBaseColor.TryGetValue(materialId, out Color baseColor))
            {
                baseColor = material.GetColor(ColorId);
                _scopeBaseColor[materialId] = baseColor;
            }

            float clampedMultiplier = Mathf.Clamp01(multiplier);
            Color dimmedColor = baseColor;
            dimmedColor.r *= clampedMultiplier;
            dimmedColor.g *= clampedMultiplier;
            dimmedColor.b *= clampedMultiplier;
            dimmedColor.a *= clampedMultiplier;
            material.SetColor(ColorId, dimmedColor);
        }
    }
}
