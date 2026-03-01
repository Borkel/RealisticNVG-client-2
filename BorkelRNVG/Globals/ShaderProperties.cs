using UnityEngine;

namespace BorkelRNVG.Globals
{
    public static class ShaderProperties
    {
        public static readonly int MaskId = Shader.PropertyToID("_Mask");
        public static readonly int InvMaskSizeId = Shader.PropertyToID("_InvMaskSize");
        public static readonly int InvAspectId = Shader.PropertyToID("_InvAspect");
        public static readonly int CameraAspectId = Shader.PropertyToID("_CameraAspect");
        public static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    }
}
