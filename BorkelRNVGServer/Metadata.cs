using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace BorkelRNVGServer
{
    public record Metadata : IModMetadata
    {
        public string ModGuid { get; init; } = "com.borkel.nvgmasks";
        public string Name { get; init; } =  "Borkel's Realistic Night Vision Goggles";
        public string Author { get; init; } = "Borkel";
        public List<string>? Contributors { get; init; } = ["Fontaine", "Mirni", "CJ", "GrooveypenguinX", "Choccster", "kiobu-kouhai", "DrakiaXYZ", "kiki", "Props", "Mattdokn"];
        public Version Version { get; init; } = new Version("2.3.0");
        public Range SptVersion { get; init; } = new Range("~4.1.0");
        public string? Url { get; init; } = "https://github.com/Borkel/RealisticNVG-client-2/";
        public string License { get; init; } = "Creative Commons BY-NC-SA 3.0";
        public List<string>? Incompatibilities { get; init; }
        public Dictionary<string, Range>? ModDependencies { get; init; }
        public bool HasPrepatcher { get; init; } = false;
    }
}
