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

        [JsonProperty("shader")]
        public RealisticNvgSettings Shader { get; set; } = new RealisticNvgSettings();
    }
}
