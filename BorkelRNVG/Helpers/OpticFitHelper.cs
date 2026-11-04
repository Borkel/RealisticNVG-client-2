using System.Collections.Generic;
using UnityEngine;

namespace BorkelRNVG.Helpers
{
    internal static class OpticFitHelper
    {
        private const byte HousingOpaqueAlpha = 200;

        // Pushes the housing's soft outer edge past the screen edge.
        private const float CoverMargin = 1.02f;

        private static readonly Dictionary<Texture2D, Vector2> LensExtents =
            new Dictionary<Texture2D, Vector2>();
        private static readonly Dictionary<Texture2D, float> HousingExtents =
            new Dictionary<Texture2D, float>();
        
        public static Vector2 GetLensExtent(Texture2D lens, float alphaCutoff)
        {
            if (lens == null)
                return Vector2.zero;
            if (LensExtents.TryGetValue(lens, out Vector2 cached))
                return cached;

            Vector2 extent = Vector2.zero;
            if (lens.isReadable)
            {
                byte cutoff = (byte)Mathf.Clamp(Mathf.RoundToInt(alphaCutoff * 255f), 0, 255);
                Color32[] pixels = lens.GetPixels32();
                int width = lens.width;
                int height = lens.height;
                float halfWidth = 0f;
                float halfHeight = 0f;
                for (int y = 0; y < height; y++)
                {
                    int row = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        if (pixels[row + x].a >= cutoff)
                            continue;
                        halfWidth = Mathf.Max(halfWidth, Mathf.Abs(x + 0.5f - width * 0.5f) + 0.5f);
                        halfHeight = Mathf.Max(halfHeight, Mathf.Abs(y + 0.5f - height * 0.5f) + 0.5f);
                    }
                }
                extent = new Vector2(halfWidth / height, halfHeight / height);
            }

            LensExtents[lens] = extent;
            return extent;
        }

        public static float GetHousingExtent(Texture2D mask)
        {
            if (mask == null)
                return 0f;
            if (HousingExtents.TryGetValue(mask, out float cached))
                return cached;

            float extent = 0f;
            if (mask.isReadable)
            {
                // Row zero of GetPixels32 is the bottom of the image.
                Color32[] pixels = mask.GetPixels32();
                int width = mask.width;
                int height = mask.height;
                for (int y = 0; y < height && extent <= 0f; y++)
                {
                    int row = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        if (pixels[row + x].a < HousingOpaqueAlpha)
                            continue;
                        extent = Mathf.Max(0f, (height * 0.5f - y) / height);
                        break;
                    }
                }
            }

            HousingExtents[mask] = extent;
            return extent;
        }

        public static float ResolveScale(float baseScale, float aspect, Vector2 center,
            Vector2 lensExtent, float housingExtent)
        {
            if (housingExtent <= 0f || lensExtent.x <= 0f || lensExtent.y <= 0f)
                return baseScale;

            float coverScale = Mathf.Max(center.y, 1f - center.y) / housingExtent * CoverMargin;
            coverScale = Mathf.Min(coverScale, Mathf.Min(center.y, 1f - center.y) / lensExtent.y);
            if (coverScale <= baseScale)
                return baseScale;

            float widthFitScale = aspect * Mathf.Min(center.x, 1f - center.x) / lensExtent.x;
            if (widthFitScale >= coverScale)
                return coverScale;
            
            if (widthFitScale >= baseScale)
                return widthFitScale;
            
            return coverScale;
        }
    }
}
