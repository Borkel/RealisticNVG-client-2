using BorkelRNVG.Enum;

namespace BorkelRNVG.Struct
{
    public struct NightVisionConfigStruct
    {
        public float Gain;
        public float NoiseIntensity;
        public float NoiseSize;
        public float MaskSize;
        public float Red;
        public float Green;
        public float Blue;
        public EGatingType GatingType;
        public float GatingSpeed;
        public float MaxBrightness;
        public float MinBrightness;
        public float MinBrightnessThreshold;
        public float MaxBrightnessThreshold;
    }
}
