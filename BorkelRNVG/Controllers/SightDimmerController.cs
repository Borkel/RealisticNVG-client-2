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
        private static readonly int MarkTexId = Shader.PropertyToID("_MarkTex");
        private static readonly FieldInfo ReticleMaterialField = typeof(OpticRetrice)
            .GetField("material_0", BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly Dictionary<int, float> _collimatorBaseHdr = new Dictionary<int, float>();
        private readonly Dictionary<int, Color> _scopeBaseColor = new Dictionary<int, Color>();
        private readonly Dictionary<int, Texture> _baseMarkTextures = new Dictionary<int, Texture>();
        private readonly Dictionary<int, Texture2D> _dimmedMarkTextures = new Dictionary<int, Texture2D>();
        private readonly Dictionary<int, float> _dimmedMarkMultipliers = new Dictionary<int, float>();
        private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();

        private float _elapsed;

        public void Tick(bool enabled, float collimatorMultiplier, float scopeMultiplier, float bakedReticleMultiplier)
        {
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < UpdateInterval)
                return;

            _elapsed = 0f;
            Apply(enabled, collimatorMultiplier, scopeMultiplier, bakedReticleMultiplier);
        }

        public void ApplyImmediately(bool enabled, float collimatorMultiplier, float scopeMultiplier, float bakedReticleMultiplier)
        {
            _elapsed = 0f;
            Apply(enabled, collimatorMultiplier, scopeMultiplier, bakedReticleMultiplier);
        }

        private void Apply(bool enabled, float collimatorMultiplier, float scopeMultiplier, float bakedReticleMultiplier)
        {
            Transform weaponRoot = PlayerHelper.LocalPlayer?.PlayerBones?.WeaponRoot?.Original;
            if (weaponRoot == null)
                return;

            bool dim = enabled && NvgHelper.IsNvgOn;
            ApplyToCollimators(weaponRoot, dim ? collimatorMultiplier : 1f);
            ApplyToScopeReticle(dim ? scopeMultiplier : 1f);
            ApplyToBakedReticles(weaponRoot, dim ? bakedReticleMultiplier : 1f);
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

        private void ApplyToBakedReticles(Transform weaponRoot, float multiplier)
        {
            float clampedMultiplier = Mathf.Clamp01(multiplier);
            OpticSight[] optics = weaponRoot.GetComponentsInChildren<OpticSight>(true);
            foreach (OpticSight optic in optics)
            {
                // Optics with a separate camera reticle are owned by the existing scope control.
                if (optic.ScopeData != null && optic.ScopeData.Reticle != null)
                    continue;

                Renderer lens = optic.LensRenderer;
                if (lens == null)
                    continue;

                Texture baseTexture = GetBaseMarkTexture(lens.sharedMaterial);
                if (baseTexture == null)
                    continue;

                Texture texture = clampedMultiplier < 1f
                    ? GetDimmedMarkTexture(baseTexture, clampedMultiplier) ?? baseTexture
                    : baseTexture;

                lens.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetTexture(MarkTexId, texture);
                lens.SetPropertyBlock(_propertyBlock);
                _propertyBlock.Clear();
            }
        }

        private Texture GetBaseMarkTexture(Material material)
        {
            if (material == null || !material.HasProperty(MarkTexId))
                return null;

            int materialId = material.GetInstanceID();
            if (!_baseMarkTextures.TryGetValue(materialId, out Texture texture))
            {
                texture = material.GetTexture(MarkTexId);
                _baseMarkTextures[materialId] = texture;
            }

            return texture;
        }

        private Texture2D GetDimmedMarkTexture(Texture source, float multiplier)
        {
            int sourceId = source.GetInstanceID();
            if (_dimmedMarkMultipliers.TryGetValue(sourceId, out float previousMultiplier) &&
                Mathf.Approximately(previousMultiplier, multiplier))
            {
                _dimmedMarkTextures.TryGetValue(sourceId, out Texture2D cached);
                return cached;
            }

            if (_dimmedMarkTextures.TryGetValue(sourceId, out Texture2D stale) && stale != null)
                Object.Destroy(stale);

            Texture2D dimmed = null;
            try
            {
                dimmed = BuildDimmedMarkTexture(source, multiplier);
            }
            catch
            {
                // Unsupported textures retain their original mark rather than breaking the optic.
            }

            _dimmedMarkTextures[sourceId] = dimmed;
            _dimmedMarkMultipliers[sourceId] = multiplier;
            return dimmed;
        }

        private static Texture2D BuildDimmedMarkTexture(Texture source, float multiplier)
        {
            RenderTexture temporary = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);
            RenderTexture previous = RenderTexture.active;

            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;

                Texture2D copy = new Texture2D(
                    source.width,
                    source.height,
                    TextureFormat.RGBA32,
                    false,
                    true);
                copy.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);

                Color[] pixels = copy.GetPixels();
                for (int index = 0; index < pixels.Length; index++)
                {
                    Color color = pixels[index];
                    color.r *= multiplier;
                    color.g *= multiplier;
                    color.b *= multiplier;
                    pixels[index] = color;
                }

                copy.SetPixels(pixels);
                copy.Apply(false, false);
                copy.wrapMode = source.wrapMode;
                copy.filterMode = source.filterMode;
                copy.hideFlags = HideFlags.HideAndDontSave;
                return copy;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
            }
        }
    }
}
