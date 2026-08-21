using Newtonsoft.Json;
using System.Collections.Generic;

namespace BorkelRNVG.Models
{
    public sealed class LensLayoutDefinition
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("displayName")]
        public string DisplayName { get; set; } = "";

        [JsonProperty("lenses")]
        public List<LensLayoutTube> Lenses { get; set; } = [];

        public LensLayoutDefinition Clone()
        {
            LensLayoutDefinition clone = new LensLayoutDefinition
            {
                Id = Id,
                DisplayName = DisplayName
            };
            if (Lenses != null)
            {
                foreach (LensLayoutTube lens in Lenses)
                    clone.Lenses.Add((lens ?? new LensLayoutTube()).Clone());
            }
            return clone;
        }
    }

    public sealed class LensLayoutTube
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("centerX")]
        public float CenterX { get; set; } = 0.5f;

        [JsonProperty("centerY")]
        public float CenterY { get; set; } = 0.5f;

        [JsonProperty("radius")]
        public float Radius { get; set; }

        [JsonProperty("distortionMultiplier")]
        public float DistortionMultiplier { get; set; } = 1f;

        [JsonProperty("fusionGroup")]
        public int FusionGroup { get; set; }

        [JsonProperty("vignetteMultiplier")]
        public float VignetteMultiplier { get; set; } = 1f;

        public LensLayoutTube Clone()
        {
            return (LensLayoutTube)MemberwiseClone();
        }
    }
}
