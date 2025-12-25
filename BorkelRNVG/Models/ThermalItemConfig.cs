using EFT;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace BorkelRNVG.Models
{
    public class ThermalItemConfig
    {
        [JsonProperty("itemId")]
        public string ItemId { get; set; } = null;

        [JsonProperty("itemIds")]
        public List<string> ItemIds { get; set; } = [];

        [JsonProperty("category")]
        public string Category { get; set; } = null;
        
        [JsonProperty("isFpsStuck")]
        public bool IsFpsStuck { get; set; } = false;
        
        [JsonProperty("minFps")]
        public int MinFps { get; set; } = 60;
        
        [JsonProperty("maxFps")]
        public int MaxFps { get; set; } = 60;
        
        [JsonProperty("isPixelated")]
        public bool IsPixelated { get; set; } = false;
        
        [JsonProperty("isNoisy")]
        public bool IsNoisy { get; set; } = false;
        
        [JsonProperty("isMotionBlurred")]
        public bool IsMotionBlurred { get; set; } = false;
    }
}
