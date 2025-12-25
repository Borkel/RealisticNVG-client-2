using System.IO;
using System.Reflection;

namespace BorkelRNVG.Globals
{
    public static class ModDirectories
    {
        public static string AssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string AssetsPath = Path.Combine(AssemblyPath, "Assets");
        public static string ShadersPath = Path.Combine(AssetsPath, "Shaders");
        public static string SoundsPath = Path.Combine(AssetsPath, "Sounds");
        public static string NvgPath = Path.Combine(AssetsPath, "NVG");
        public static string ThermalPath = Path.Combine(AssetsPath, "Thermal");
        public static string CommonAssetsPath = Path.Combine(AssetsPath, "CommonAssets");
    }
}
