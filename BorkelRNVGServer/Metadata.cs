using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace BorkelRNVGServer
{
    public record Metadata : AbstractModMetadata
    {
        public override string ModGuid { get; init; } = "com.borkel.nvgmasks";
        public override string Name { get; init; } =  "Borkel's Realistic Night Vision Goggles";
        public override string Author { get; init; } = "Borkel";
        public override List<string>? Contributors { get; init; } = ["Fontaine", "Mirni", "CJ", "GrooveypenguinX", "Choccster", "kiobu-kouhai", "DrakiaXYZ", "kiki", "Props", "Mattdokn"];
        public override Version Version { get; init; } = new Version("2.0.1");
        public override Range SptVersion { get; init; } = new Range("~4.0.0");
        public override string? Url { get; init; } = "https://github.com/Borkel/RealisticNVG-client-2/";
        public override bool? IsBundleMod { get; init; } = true;
        public override string License { get; init; } = "Creative Commons BY-NC-SA 3.0";
        
        public override List<string>? Incompatibilities { get; init; }
        public override Dictionary<string, Range>? ModDependencies { get; init; }
    }
}
