using BorkelRNVG.Globals;
using BSG.CameraEffects;

namespace BorkelRNVG.Controllers.Extensions
{
    public static class NightVisionExtensions
    {
        public static void UpdateIntensity(this NightVision nightVision)
        {
            nightVision.Material_0.SetFloat(ShaderProperties.IntensityId, nightVision.Intensity);
        }
    }
}
