using BorkelRNVG.Enum;
using EFT;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace BorkelRNVG.Models
{
    public class NvgItemConfig
    {
        [JsonProperty("itemId")]
        public string ItemId { get; set; } = "";

        [JsonProperty("itemIds")]
        public List<string> ItemIds { get; set; } = [];

        [JsonProperty("category")]
        public string Category { get; set; } = "";

        [JsonProperty("gain")]
        public float Gain { get; set; } = 2.4f;
        
        [JsonProperty("noiseIntensity")]
        public float NoiseIntensity { get; set; } = 0.2f;
        
        [JsonProperty("noiseSize")]
        public float NoiseSize { get; set; } = 0.1f;
        
        [JsonProperty("maskSize")]
        public float MaskSize { get; set; } = 1f;
        
        [JsonProperty("red")]
        public float Red { get; set; } = 95f;
        
        [JsonProperty("green")]
        public float Green { get; set; } = 210f;
        
        [JsonProperty("blue")]
        public float Blue { get; set; } = 255f;

        [JsonProperty("gatingType")]
        public EGatingType GatingType { get; set; } = EGatingType.Off;
        
        [JsonProperty("gatingSpeed")]
        public float GatingSpeed { get; set; } = 0.3f;
        
        [JsonProperty("minBrightness")]
        public float MinBrightness { get; set; } = 0.2f;
        
        [JsonProperty("maxBrightness")]
        public float MaxBrightness { get; set; } = 1f;
        
        [JsonProperty("minBrightnessThreshold")]
        public float MinBrightnessThreshold { get; set; } = 0f;
        
        [JsonProperty("maxBrightnessThreshold")]
        public float MaxBrightnessThreshold { get; set; } = 0.15f;
    }
}
