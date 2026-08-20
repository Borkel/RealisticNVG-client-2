// Texture-driven image-intensifier simulation for Unity's Built-in Render Pipeline.
// RealisticNightVisionEffect.cs owns every parameter and render pass.
Shader "Hidden/CustomNightVision"
{
    Properties
    {
        [HideInInspector] _MainTex ("Source", 2D) = "white" {}
        [HideInInspector] [NoScaleOffset] _OpticalHazeTex ("Optical haze", 2D) = "black" {}
        [NoScaleOffset] _LensTexture ("Lens alpha texture", 2D) = "white" {}
        [NoScaleOffset] _MaskOverlay ("Final RGBA mask overlay", 2D) = "black" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;

    UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
    float4 _CameraDepthTexture_TexelSize;

    sampler2D _NearBlurTex;
    sampler2D _OpticalHazeTex;
    float4 _OpticalHazeTex_TexelSize;
    sampler2D _BloomTex;
    sampler2D _BloomWideTex;
    sampler2D _ExposureTex;
    sampler2D _ExposureHistory;
    sampler2D _LensTexture;
    sampler2D _MaskOverlay;

    float4 _SourceSize;
    float2 _SourceToTargetScale;
    float2 _BlurDirection;
    float _BlurDomainIsolation;
    float _BlurPackFusionChannels;
    float _OpticalHazeEnabled;
    float _OpticalHazeTextureAvailable;
    float _OpticalHazeCenterStrength;
    float _OpticalHazeEdgeStrength;
    float _OpticalHazeEdgeWidthPixels;
    float _OpticalHazeFalloff;
    float _OpticalHazeVeilStrength;
    float _ChromaticAberrationPixels;

    float2 _LensCenter;
    float _OpticTextureAspect;
    float _OpticTextureScale;
    float _LensAlphaCutoff;
    float _LensAlphaFeather;

    float4 _LensDefinition0;
    float4 _LensDefinition1;
    float4 _LensDefinition2;
    float4 _LensDefinition3;
    float4 _LensDefinitionEnabled;
    float4 _LensFusionGroups;
    float4 _LensVignetteMultipliers;
    float _LensEdgeDistortionEnabled;
    float _MultiLensDistortionStrengthPixels;
    float _MultiLensDistortionWidthPixels;
    float _MultiLensDistortionFalloff;
    float _LensSeamMode;
    float _LensSeamWidth;
    float _LensSeamOpacity;
    float4 _LensSeamColor;
    float _MultiLensVignetteEnabled;
    float _MultiLensOuterVignetteWidth;
    float _MultiLensOuterVignetteStrength;
    float _MultiLensVignetteFalloff;

    float _NearBlurEnabled;
    float _NearBlurStart;
    float _NearBlurEnd;
    float _NearBlurStrength;
    float _ForegroundExpansion;

    float _BloomEnabled;
    float _BloomThreshold;
    float _BloomSoftKnee;
    float _BloomIntensity;

    float3 _SpectralSensitivity;
    float4 _PhosphorTint;
    float _ManualGain;

    float _AutoExposure;
    float _ExposureTarget;
    float2 _ExposureEVMinMax;
    float2 _AdaptationSpeeds;
    float _HighlightProtection;
    float _ExposureHistoryValid;
    float _DeltaTime;

    float _ReadNoise;
    float _ShotNoise;
    float _CoarseNoise;
    float _FixedPatternNoise;
    float _Scintillation;
    float _ScintillationDensity;
    float _NoiseFadeStartLuminance;
    float _NoiseFadeEndLuminance;
    float _GrainPixelSize;
    float _NoiseRefreshRate;
    float _EffectTime;

    struct v2f
    {
        float4 position : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    struct LensDomainData
    {
        float valid;
        float ownerIndex;
        float distortionWeight;
        float seamWeight;
        float outerEdgeDistance;
        float2 outwardNormal;
        float distortionMultiplier;
        float outerVignetteMultiplier;
    };

    v2f Vert(appdata_img v)
    {
        v2f o;
        o.position = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord;
        return o;
    }

    float2 OrientedUV(float2 uv, float4 texelSize)
    {
        #if UNITY_UV_STARTS_AT_TOP
        if (texelSize.y < 0.0)
            uv.y = 1.0 - uv.y;
        #endif
        return uv;
    }

    float2 NearestTexelCenter(float2 screenUV, float4 texelSize)
    {
        float2 textureSize = max(abs(texelSize.zw), 1.0);
        float2 pixel = floor(saturate(screenUV) * textureSize);
        pixel = min(pixel, textureSize - 1.0);
        return (pixel + 0.5) / textureSize;
    }

    float4 SampleSource(float2 screenUV)
    {
        return tex2D(
            _MainTex,
            OrientedUV(screenUV, _MainTex_TexelSize)
        );
    }

    float SampleRawDepth(float2 screenUV)
    {
        float2 depthUV = OrientedUV(screenUV, _CameraDepthTexture_TexelSize);
        return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, depthUV);
    }

    float EyeDepth(float2 screenUV)
    {
        return LinearEyeDepth(SampleRawDepth(screenUV));
    }

    float NearCoC(float2 screenUV)
    {
        float depth = EyeDepth(screenUV);
        return 1.0 - smoothstep(
            _NearBlurStart,
            max(_NearBlurEnd, _NearBlurStart + 0.001),
            depth);
    }

    float Luminance(float3 color)
    {
        float3 sensitivity = max(_SpectralSensitivity, 0.0);
        sensitivity /= max(dot(sensitivity, float3(1.0, 1.0, 1.0)), 0.0001);
        return dot(max(color, 0.0), sensitivity);
    }

    float Hash12(float2 p)
    {
        float3 p3 = frac(float3(p.xyx) * 0.1031);
        p3 += dot(p3, p3.yzx + 33.33);
        return frac((p3.x + p3.y) * p3.z);
    }

    float Hash13(float3 p3)
    {
        p3 = frac(p3 * 0.1031);
        p3 += dot(p3, p3.zyx + 31.32);
        return frac((p3.x + p3.y) * p3.z);
    }

    float ValueNoise(float2 p)
    {
        float2 cell = floor(p);
        float2 f = frac(p);
        f = f * f * (3.0 - 2.0 * f);
        float a = Hash12(cell);
        float b = Hash12(cell + float2(1.0, 0.0));
        float c = Hash12(cell + float2(0.0, 1.0));
        float d = Hash12(cell + 1.0);
        return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
    }

    float SamePixel(float2 a, float2 b)
    {
        float2 difference = abs(a - b);
        return 1.0 - step(0.5, max(difference.x, difference.y));
    }

    float2 CardinalDirection(float randomValue)
    {
        float choice = floor(saturate(randomValue) * 3.9999);
        if (choice < 0.5)
            return float2(1.0, 0.0);
        if (choice < 1.5)
            return float2(-1.0, 0.0);
        if (choice < 2.5)
            return float2(0.0, 1.0);
        return float2(0.0, -1.0);
    }

    float2 KeepStepInsideTile(float2 position, float2 direction)
    {
        float2 nextPosition = position + direction;
        if (nextPosition.x < 0.0 || nextPosition.x > 2.0 ||
            nextPosition.y < 0.0 || nextPosition.y > 2.0)
            return -direction;
        return direction;
    }

    float GaussianHash(float2 cell, float frame)
    {
        float h0 = Hash12(cell + float2(frame * 0.071, frame * 0.113));
        float h1 = Hash12(cell.yx + float2(frame * 0.173, frame * 0.197) + 19.19);
        float h2 = Hash12(cell * 1.731 + float2(frame * 0.233, frame * 0.269) + 47.47);
        float h3 = Hash12(cell.yx * 2.113 + float2(frame * 0.307, frame * 0.331) + 73.73);
        return (h0 + h1 + h2 + h3 - 2.0) * 1.7320508;
    }

    float2 OpticTextureUV(float2 screenUV)
    {
        float screenAspect = _SourceSize.x / max(_SourceSize.y, 1.0);
        float2 centered = screenUV - _LensCenter;
        centered.x *= screenAspect / max(_OpticTextureAspect, 0.0001);
        return centered / max(_OpticTextureScale, 0.0001) + 0.5;
    }

    float TextureUVBounds(float2 uv)
    {
        float2 aboveMinimum = step(float2(0.0, 0.0), uv);
        float2 belowMaximum = step(uv, float2(1.0, 1.0));
        return aboveMinimum.x * aboveMinimum.y * belowMaximum.x * belowMaximum.y;
    }

    float EffectLensMask(float2 screenUV)
    {
        float2 lensUV = OpticTextureUV(screenUV);
        float alpha = tex2D(_LensTexture, saturate(lensUV)).a;
        float feather = max(_LensAlphaFeather, 0.00001);
        float transparentArea = 1.0 - smoothstep(
            _LensAlphaCutoff - feather,
            _LensAlphaCutoff + feather,
            alpha);
        return saturate(transparentArea) * TextureUVBounds(lensUV);
    }

    float3 ApplyMaskOverlay(float3 baseColor, float2 screenUV)
    {
        float2 overlayUV = OpticTextureUV(screenUV);
        // Clamping deliberately extends an authored opaque housing to the sides.
        float4 overlay = tex2D(_MaskOverlay, saturate(overlayUV));
        return lerp(baseColor, overlay.rgb, saturate(overlay.a));
    }

    float4 LensDefinitionAt(float index)
    {
        float4 result = _LensDefinition0;
        if (index > 0.5)
            result = _LensDefinition1;
        if (index > 1.5)
            result = _LensDefinition2;
        if (index > 2.5)
            result = _LensDefinition3;
        return result;
    }

    float LensDefinitionEnabledAt(float index)
    {
        float result = _LensDefinitionEnabled.x;
        if (index > 0.5)
            result = _LensDefinitionEnabled.y;
        if (index > 1.5)
            result = _LensDefinitionEnabled.z;
        if (index > 2.5)
            result = _LensDefinitionEnabled.w;
        return step(0.5, result);
    }

    float LensFusionGroupAt(float index)
    {
        float result = _LensFusionGroups.x;
        if (index > 0.5)
            result = _LensFusionGroups.y;
        if (index > 1.5)
            result = _LensFusionGroups.z;
        if (index > 2.5)
            result = _LensFusionGroups.w;
        return result;
    }

    float LensDividerEnabled(float firstIndex, float secondIndex)
    {
        float lower = min(firstIndex, secondIndex);
        float upper = max(firstIndex, secondIndex);
        float sidePair = 0.0;

        if (lower < 0.5 && upper > 0.5 && upper < 1.5)
            sidePair = 1.0;
        else if (lower > 1.5 && lower < 2.5 && upper > 2.5)
            sidePair = 1.0;

        float groupsDiffer = step(
            0.25,
            abs(LensFusionGroupAt(firstIndex) -
                LensFusionGroupAt(secondIndex)));
        float bothEnabled = LensDefinitionEnabledAt(firstIndex) *
            LensDefinitionEnabledAt(secondIndex);
        return sidePair * groupsDiffer * bothEnabled;
    }

    float LensVignetteMultiplierAt(float index)
    {
        float result = _LensVignetteMultipliers.x;
        if (index > 0.5)
            result = _LensVignetteMultipliers.y;
        if (index > 1.5)
            result = _LensVignetteMultipliers.z;
        if (index > 2.5)
            result = _LensVignetteMultipliers.w;
        return max(result, 0.0);
    }

    float2 LensPhysicalOffset(float2 opticUV, float2 centerUV)
    {
        float2 offset = opticUV - centerUV;
        offset.x *= max(_OpticTextureAspect, 0.0001);
        return offset;
    }

    float LensPower(float2 opticUV, float4 definition)
    {
        float2 offset = LensPhysicalOffset(opticUV, definition.xy);
        return dot(offset, offset) - definition.z * definition.z;
    }

    float OpticDistanceToPixels(float distance)
    {
        return distance *
            max(_OpticTextureScale, 0.0001) *
            max(_SourceSize.y, 1.0);
    }

    void ConsiderFusionOuterDistance(
        float candidateIndex,
        float4 candidateDefinition,
        float candidateEnabled,
        float2 opticUV,
        float ownerFusionGroup,
        inout float outerDistance)
    {
        if (candidateEnabled < 0.5 ||
            abs(LensFusionGroupAt(candidateIndex) - ownerFusionGroup) > 0.25)
            return;

        float2 offset = LensPhysicalOffset(opticUV, candidateDefinition.xy);
        float radialDistance = length(offset);
        if (radialDistance > candidateDefinition.z)
            return;

        // The visible edge of a fused overlap is the union boundary.
        float clearance = candidateDefinition.z - radialDistance;
        outerDistance = max(outerDistance, clearance);
    }

    void AccumulateFusionDistortionFrame(
        float candidateIndex,
        float4 candidateDefinition,
        float candidateEnabled,
        float2 opticUV,
        float ownerFusionGroup,
        float outerDistance,
        inout float2 normalSum,
        inout float multiplierSum,
        inout float weightSum)
    {
        if (candidateEnabled < 0.5 ||
            abs(LensFusionGroupAt(candidateIndex) - ownerFusionGroup) > 0.25)
            return;

        float2 offset = LensPhysicalOffset(opticUV, candidateDefinition.xy);
        float radialDistance = length(offset);
        if (radialDistance > candidateDefinition.z)
            return;

        float clearance = candidateDefinition.z - radialDistance;
        float clearanceDeltaPixels = OpticDistanceToPixels(
            max(outerDistance - clearance, 0.0));
        float handoffWidthPixels = max(
            abs(_MultiLensDistortionWidthPixels) * 0.2,
            1.0);
        float weight = 1.0 - smoothstep(
            0.0,
            handoffWidthPixels,
            clearanceDeltaPixels);

        normalSum += offset / max(radialDistance, 0.00001) * weight;
        multiplierSum += candidateDefinition.w * weight;
        weightSum += weight;
    }

    void AccumulateFusionVignette(
        float candidateIndex,
        float4 candidateDefinition,
        float candidateEnabled,
        float2 opticUV,
        float ownerFusionGroup,
        float outerDistance,
        inout float vignetteSum,
        inout float weightSum)
    {
        if (candidateEnabled < 0.5 ||
            abs(LensFusionGroupAt(candidateIndex) - ownerFusionGroup) > 0.25)
            return;

        float radialDistance = length(LensPhysicalOffset(
            opticUV, candidateDefinition.xy));
        float clearance = candidateDefinition.z - radialDistance;
        float blendWidth = max(_MultiLensOuterVignetteWidth * 0.15, 0.002);
        float weight = 1.0 - smoothstep(
            0.0,
            blendWidth,
            max(outerDistance - clearance, 0.0));
        vignetteSum += LensVignetteMultiplierAt(candidateIndex) * weight;
        weightSum += weight;
    }

    void ChooseLensOwner(
        float power,
        float inside,
        float index,
        inout float bestPower,
        inout float ownerIndex)
    {
        if (inside > 0.5 && power < bestPower)
        {
            bestPower = power;
            ownerIndex = index;
        }
    }

    void ConsiderLensSeam(
        float candidateIndex,
        float4 candidateDefinition,
        float candidatePower,
        float candidateInside,
        float ownerIndex,
        float4 ownerDefinition,
        float ownerPower,
        inout float seamValid,
        inout float seamDistance)
    {
        if (candidateInside < 0.5 ||
            abs(candidateIndex - ownerIndex) < 0.25 ||
            LensDividerEnabled(ownerIndex, candidateIndex) < 0.5)
            return;

        float2 ownerCenter = ownerDefinition.xy - 0.5;
        float2 candidateCenter = candidateDefinition.xy - 0.5;
        ownerCenter.x *= max(_OpticTextureAspect, 0.0001);
        candidateCenter.x *= max(_OpticTextureAspect, 0.0001);
        float2 centerDelta = candidateCenter - ownerCenter;
        float centerDistance = length(centerDelta);

        float intersects = step(
            abs(ownerDefinition.z - candidateDefinition.z) + 0.00001,
            centerDistance);
        intersects *= step(
            centerDistance,
            ownerDefinition.z + candidateDefinition.z - 0.00001);
        if (intersects < 0.5 || centerDistance < 0.00001)
            return;

        float distanceToRadicalAxis = max(candidatePower - ownerPower, 0.0) /
            max(2.0 * centerDistance, 0.00001);
        if (distanceToRadicalAxis < seamDistance)
        {
            seamValid = 1.0;
            seamDistance = distanceToRadicalAxis;
        }
    }

    LensDomainData EvaluateLensDomain(float2 screenUV)
    {
        LensDomainData domain;
        domain.valid = 0.0;
        domain.ownerIndex = -1.0;
        domain.distortionWeight = 0.0;
        domain.seamWeight = 0.0;
        domain.outerEdgeDistance = 1000000.0;
        domain.outwardNormal = float2(0.0, 0.0);
        domain.distortionMultiplier = 0.0;
        domain.outerVignetteMultiplier = 0.0;

        float2 opticUV = OpticTextureUV(screenUV);
        if (TextureUVBounds(opticUV) < 0.5)
            return domain;

        float4 powers = float4(1000000.0, 1000000.0, 1000000.0, 1000000.0);
        float4 inside = float4(0.0, 0.0, 0.0, 0.0);

        if (_LensDefinitionEnabled.x > 0.5)
        {
            powers.x = LensPower(opticUV, _LensDefinition0);
            inside.x = step(powers.x, 0.0);
        }
        if (_LensDefinitionEnabled.y > 0.5)
        {
            powers.y = LensPower(opticUV, _LensDefinition1);
            inside.y = step(powers.y, 0.0);
        }
        if (_LensDefinitionEnabled.z > 0.5)
        {
            powers.z = LensPower(opticUV, _LensDefinition2);
            inside.z = step(powers.z, 0.0);
        }
        if (_LensDefinitionEnabled.w > 0.5)
        {
            powers.w = LensPower(opticUV, _LensDefinition3);
            inside.w = step(powers.w, 0.0);
        }

        float bestPower = 1000000.0;
        float ownerIndex = -1.0;
        ChooseLensOwner(powers.x, inside.x, 0.0, bestPower, ownerIndex);
        ChooseLensOwner(powers.y, inside.y, 1.0, bestPower, ownerIndex);
        ChooseLensOwner(powers.z, inside.z, 2.0, bestPower, ownerIndex);
        ChooseLensOwner(powers.w, inside.w, 3.0, bestPower, ownerIndex);
        if (ownerIndex < -0.5)
            return domain;

        float4 ownerDefinition = LensDefinitionAt(ownerIndex);
        float ownerFusionGroup = LensFusionGroupAt(ownerIndex);
        float outerDistance = 0.0;

        ConsiderFusionOuterDistance(
            0.0, _LensDefinition0, _LensDefinitionEnabled.x,
            opticUV, ownerFusionGroup, outerDistance);
        ConsiderFusionOuterDistance(
            1.0, _LensDefinition1, _LensDefinitionEnabled.y,
            opticUV, ownerFusionGroup, outerDistance);
        ConsiderFusionOuterDistance(
            2.0, _LensDefinition2, _LensDefinitionEnabled.z,
            opticUV, ownerFusionGroup, outerDistance);
        ConsiderFusionOuterDistance(
            3.0, _LensDefinition3, _LensDefinitionEnabled.w,
            opticUV, ownerFusionGroup, outerDistance);

        float2 outerNormalSum = float2(0.0, 0.0);
        float outerDistortionMultiplierSum = 0.0;
        float outerDistortionWeightSum = 0.0;
        AccumulateFusionDistortionFrame(
            0.0, _LensDefinition0, _LensDefinitionEnabled.x,
            opticUV, ownerFusionGroup, outerDistance,
            outerNormalSum, outerDistortionMultiplierSum,
            outerDistortionWeightSum);
        AccumulateFusionDistortionFrame(
            1.0, _LensDefinition1, _LensDefinitionEnabled.y,
            opticUV, ownerFusionGroup, outerDistance,
            outerNormalSum, outerDistortionMultiplierSum,
            outerDistortionWeightSum);
        AccumulateFusionDistortionFrame(
            2.0, _LensDefinition2, _LensDefinitionEnabled.z,
            opticUV, ownerFusionGroup, outerDistance,
            outerNormalSum, outerDistortionMultiplierSum,
            outerDistortionWeightSum);
        AccumulateFusionDistortionFrame(
            3.0, _LensDefinition3, _LensDefinitionEnabled.w,
            opticUV, ownerFusionGroup, outerDistance,
            outerNormalSum, outerDistortionMultiplierSum,
            outerDistortionWeightSum);

        float2 outerNormal = outerNormalSum /
            max(outerDistortionWeightSum, 0.00001);
        outerNormal /= max(length(outerNormal), 0.00001);
        float outerDistortionMultiplier =
            outerDistortionMultiplierSum /
            max(outerDistortionWeightSum, 0.00001);

        float outerVignetteSum = 0.0;
        float outerVignetteWeight = 0.0;
        AccumulateFusionVignette(
            0.0, _LensDefinition0, _LensDefinitionEnabled.x,
            opticUV, ownerFusionGroup, outerDistance,
            outerVignetteSum, outerVignetteWeight);
        AccumulateFusionVignette(
            1.0, _LensDefinition1, _LensDefinitionEnabled.y,
            opticUV, ownerFusionGroup, outerDistance,
            outerVignetteSum, outerVignetteWeight);
        AccumulateFusionVignette(
            2.0, _LensDefinition2, _LensDefinitionEnabled.z,
            opticUV, ownerFusionGroup, outerDistance,
            outerVignetteSum, outerVignetteWeight);
        AccumulateFusionVignette(
            3.0, _LensDefinition3, _LensDefinitionEnabled.w,
            opticUV, ownerFusionGroup, outerDistance,
            outerVignetteSum, outerVignetteWeight);
        float outerVignetteMultiplier = outerVignetteSum /
            max(outerVignetteWeight, 0.00001);

        float seamValid = 0.0;
        float seamDistance = 1000000.0;
        ConsiderLensSeam(
            0.0, _LensDefinition0, powers.x, inside.x,
            ownerIndex, ownerDefinition, bestPower,
            seamValid, seamDistance);
        ConsiderLensSeam(
            1.0, _LensDefinition1, powers.y, inside.y,
            ownerIndex, ownerDefinition, bestPower,
            seamValid, seamDistance);
        ConsiderLensSeam(
            2.0, _LensDefinition2, powers.z, inside.z,
            ownerIndex, ownerDefinition, bestPower,
            seamValid, seamDistance);
        ConsiderLensSeam(
            3.0, _LensDefinition3, powers.w, inside.w,
            ownerIndex, ownerDefinition, bestPower,
            seamValid, seamDistance);

        float seamBand = seamValid * (1.0 - smoothstep(
            0.0, max(_LensSeamWidth, 0.00001), seamDistance));
        float outerDistancePixels = OpticDistanceToPixels(outerDistance);
        float edgeBand = 1.0 - smoothstep(
            0.0,
            max(_MultiLensDistortionWidthPixels, 0.00001),
            outerDistancePixels);

        domain.valid = 1.0;
        domain.ownerIndex = ownerIndex;
        domain.distortionWeight = pow(
            saturate(edgeBand),
            max(_MultiLensDistortionFalloff, 0.01));
        domain.seamWeight = saturate(seamBand);
        domain.outerEdgeDistance = outerDistance;
        domain.outwardNormal = outerNormal;
        domain.distortionMultiplier = outerDistortionMultiplier;
        domain.outerVignetteMultiplier = outerVignetteMultiplier;
        return domain;
    }

    float LensOwnerAtOpticUV(float2 opticUV)
    {
        float bestPower = 1000000.0;
        float ownerIndex = -1.0;
        float power = 0.0;

        if (_LensDefinitionEnabled.x > 0.5)
        {
            power = LensPower(opticUV, _LensDefinition0);
            ChooseLensOwner(power, step(power, 0.0), 0.0, bestPower, ownerIndex);
        }
        if (_LensDefinitionEnabled.y > 0.5)
        {
            power = LensPower(opticUV, _LensDefinition1);
            ChooseLensOwner(power, step(power, 0.0), 1.0, bestPower, ownerIndex);
        }
        if (_LensDefinitionEnabled.z > 0.5)
        {
            power = LensPower(opticUV, _LensDefinition2);
            ChooseLensOwner(power, step(power, 0.0), 2.0, bestPower, ownerIndex);
        }
        if (_LensDefinitionEnabled.w > 0.5)
        {
            power = LensPower(opticUV, _LensDefinition3);
            ChooseLensOwner(power, step(power, 0.0), 3.0, bestPower, ownerIndex);
        }
        return ownerIndex;
    }

    float LensOwnerOnly(float2 screenUV)
    {
        float2 opticUV = OpticTextureUV(screenUV);
        if (TextureUVBounds(opticUV) < 0.5 ||
            EffectLensMask(screenUV) <= 0.0001)
            return -1.0;
        return LensOwnerAtOpticUV(opticUV);
    }

    float4 SampleOpticalHazeTexel(float2 pixel, float2 textureSize)
    {
        pixel = clamp(pixel, 0.0, textureSize - 1.0);
        float2 screenUV = (pixel + 0.5) / textureSize;
        return tex2D(
            _OpticalHazeTex,
            OrientedUV(screenUV, _OpticalHazeTex_TexelSize));
    }

    float OpticalHazeTagMatch(float sampleTag, float expectedTag)
    {
        // Fifths remain distinct in both floating-point and UNorm fallback RTs.
        return 1.0 - step(0.01, abs(sampleTag - expectedTag));
    }

    void AccumulateOpticalHazeTexel(
        float2 pixel,
        float bilinearWeight,
        float2 textureSize,
        float expectedTag,
        inout float3 colorSum,
        inout float weightSum)
    {
        float4 sampleValue = SampleOpticalHazeTexel(pixel, textureSize);
        float acceptedWeight = bilinearWeight *
            OpticalHazeTagMatch(sampleValue.a, expectedTag);
        colorSum += sampleValue.rgb * acceptedWeight;
        weightSum += acceptedWeight;
    }

    float3 SampleOpticalHaze(float2 screenUV, float3 sharpColor)
    {
        if (_OpticalHazeTextureAvailable < 0.5)
            return sharpColor;

        float centerOwner = LensOwnerOnly(screenUV);
        if (centerOwner < -0.5)
            return sharpColor;

        float expectedTag =
            (LensFusionGroupAt(centerOwner) + 1.0) * 0.2;
        float2 textureSize = max(
            abs(_OpticalHazeTex_TexelSize.zw),
            1.0);
        float2 texturePosition = saturate(screenUV) * textureSize - 0.5;
        float2 basePixel = floor(texturePosition);
        float2 blend = frac(texturePosition);

        float3 colorSum = float3(0.0, 0.0, 0.0);
        float weightSum = 0.0;
        AccumulateOpticalHazeTexel(
            basePixel,
            (1.0 - blend.x) * (1.0 - blend.y),
            textureSize,
            expectedTag,
            colorSum,
            weightSum);
        AccumulateOpticalHazeTexel(
            basePixel + float2(1.0, 0.0),
            blend.x * (1.0 - blend.y),
            textureSize,
            expectedTag,
            colorSum,
            weightSum);
        AccumulateOpticalHazeTexel(
            basePixel + float2(0.0, 1.0),
            (1.0 - blend.x) * blend.y,
            textureSize,
            expectedTag,
            colorSum,
            weightSum);
        AccumulateOpticalHazeTexel(
            basePixel + float2(1.0, 1.0),
            blend.x * blend.y,
            textureSize,
            expectedTag,
            colorSum,
            weightSum);

        if (weightSum <= 0.00001)
            return sharpColor;
        return colorSum / weightSum;
    }

    float LensTopologyTapWeight(float centerOwner, float2 sampleScreenUV)
    {
        float sampleOwner = LensOwnerOnly(sampleScreenUV);
        if (sampleOwner < -0.5)
            return 0.0;
        float centerGroup = LensFusionGroupAt(centerOwner);
        float sampleGroup = LensFusionGroupAt(sampleOwner);
        return 1.0 - step(0.25, abs(sampleGroup - centerGroup));
    }

    float3 SampleFusionSafeSource(
        float2 screenUV,
        float centerOwner,
        float3 fallbackColor,
        out float sampleValid)
    {
        sampleValid = 0.0;
        if (centerOwner < -0.5)
            return fallbackColor;

        float2 textureSize = max(abs(_MainTex_TexelSize.zw), 1.0);
        float2 boundedUV = saturate(screenUV);
        float2 texturePosition = boundedUV * textureSize - 0.5;
        float2 basePixel = floor(texturePosition);
        float2 blend = frac(texturePosition);

        float2 pixel0 = clamp(basePixel, 0.0, textureSize - 1.0);
        float2 pixel1 = clamp(
            basePixel + float2(1.0, 0.0), 0.0, textureSize - 1.0);
        float2 pixel2 = clamp(
            basePixel + float2(0.0, 1.0), 0.0, textureSize - 1.0);
        float2 pixel3 = clamp(
            basePixel + float2(1.0, 1.0), 0.0, textureSize - 1.0);
        float support =
            (1.0 - blend.x) * (1.0 - blend.y) *
                LensTopologyTapWeight(
                    centerOwner, (pixel0 + 0.5) / textureSize) +
            blend.x * (1.0 - blend.y) *
                LensTopologyTapWeight(
                    centerOwner, (pixel1 + 0.5) / textureSize) +
            (1.0 - blend.x) * blend.y *
                LensTopologyTapWeight(
                    centerOwner, (pixel2 + 0.5) / textureSize) +
            blend.x * blend.y *
                LensTopologyTapWeight(
                    centerOwner, (pixel3 + 0.5) / textureSize);

        if (support < 0.99999)
            return fallbackColor;
        sampleValid = 1.0;
        return SampleSource(boundedUV).rgb;
    }

    float LensBlurTapWeight(float centerOwner, float2 sampleScreenUV)
    {
        if (_BlurDomainIsolation < 0.5)
            return 1.0;
        return LensTopologyTapWeight(centerOwner, sampleScreenUV);
    }

    float4 LensFusionChannelMask(float ownerIndex)
    {
        float group = LensFusionGroupAt(ownerIndex);
        return 1.0 - step(
            float4(0.25, 0.25, 0.25, 0.25),
            abs(float4(0.0, 1.0, 2.0, 3.0) - group));
    }

    float BloomSignalFromSample(float4 bloomSample, float ownerIndex)
    {
        return ownerIndex > -0.5
            ? dot(bloomSample, LensFusionChannelMask(ownerIndex))
            : 0.0;
    }

    float2 DistortedUV(float2 uv, out LensDomainData domain)
    {
        domain = EvaluateLensDomain(uv);
        if (domain.valid > 0.5 && _LensEdgeDistortionEnabled > 0.5)
        {
            float screenAspect = _SourceSize.x / max(_SourceSize.y, 1.0);
            float2 screenNormal = float2(
                domain.outwardNormal.x / max(screenAspect, 0.0001),
                domain.outwardNormal.y);
            float warpPixels = _MultiLensDistortionStrengthPixels *
                domain.distortionMultiplier * domain.distortionWeight;
            float warpUVPerScreenHeight = warpPixels /
                max(_SourceSize.y, 1.0);
            return clamp(
                uv - screenNormal * warpUVPerScreenHeight,
                0.0,
                1.0);
        }
        return uv;
    }

    float4 NearPremultipliedSample(float2 screenUV)
    {
        screenUV = clamp(screenUV, 0.0, 1.0);
        float coc = NearCoC(screenUV);
        float3 color = SampleSource(screenUV).rgb;
        return float4(color * coc, coc);
    }

    float3 BloomPrefilter(float3 color)
    {
        float brightness = max(color.r, max(color.g, color.b));
        float knee = max(_BloomThreshold * _BloomSoftKnee, 0.00001);
        float soft = clamp(
            brightness - _BloomThreshold + knee,
            0.0,
            2.0 * knee);
        soft = soft * soft / (4.0 * knee + 0.00001);
        float contribution = max(brightness - _BloomThreshold, soft) /
            max(brightness, 0.00001);
        return max(color, 0.0) * saturate(contribution);
    }

    float4 FragNearPrepare(v2f i) : SV_Target
    {
        float2 footprint = abs(_MainTex_TexelSize.xy) *
            _SourceToTargetScale * 0.32;
        float4 result = NearPremultipliedSample(i.uv) * 4.0;
        result += NearPremultipliedSample(
            i.uv + float2( footprint.x, 0.0)) * 2.0;
        result += NearPremultipliedSample(
            i.uv + float2(-footprint.x, 0.0)) * 2.0;
        result += NearPremultipliedSample(
            i.uv + float2(0.0,  footprint.y)) * 2.0;
        result += NearPremultipliedSample(
            i.uv + float2(0.0, -footprint.y)) * 2.0;
        result += NearPremultipliedSample(
            i.uv + float2( footprint.x,  footprint.y));
        result += NearPremultipliedSample(
            i.uv + float2(-footprint.x,  footprint.y));
        result += NearPremultipliedSample(
            i.uv + float2( footprint.x, -footprint.y));
        result += NearPremultipliedSample(
            i.uv + float2(-footprint.x, -footprint.y));
        return result * (1.0 / 16.0);
    }

    float4 FragGaussian(v2f i) : SV_Target
    {
        float2 offset = abs(_MainTex_TexelSize.xy) * _BlurDirection;
        float centerOwner = -1.0;
        if (_BlurDomainIsolation > 0.5)
        {
            centerOwner = LensOwnerOnly(i.uv);
            if (centerOwner < -0.5)
            {
                if (_BlurPackFusionChannels > 0.5)
                    return float4(0.0, 0.0, 0.0, 0.0);
                float2 centerUV = NearestTexelCenter(
                    i.uv,
                    _MainTex_TexelSize);
                float3 centerColor = tex2D(
                    _MainTex,
                    OrientedUV(centerUV, _MainTex_TexelSize)).rgb;
                return float4(centerColor, 0.0);
            }
        }

        float2 uv0 = clamp(i.uv, 0.0, 1.0);
        float2 uv1 = clamp(i.uv + offset * 1.38461538, 0.0, 1.0);
        float2 uv2 = clamp(i.uv - offset * 1.38461538, 0.0, 1.0);
        float2 uv3 = clamp(i.uv + offset * 3.23076923, 0.0, 1.0);
        float2 uv4 = clamp(i.uv - offset * 3.23076923, 0.0, 1.0);

        if (_BlurDomainIsolation > 0.5 &&
            _BlurPackFusionChannels < 0.5)
        {
            uv0 = NearestTexelCenter(uv0, _MainTex_TexelSize);
            uv1 = NearestTexelCenter(uv1, _MainTex_TexelSize);
            uv2 = NearestTexelCenter(uv2, _MainTex_TexelSize);
            uv3 = NearestTexelCenter(uv3, _MainTex_TexelSize);
            uv4 = NearestTexelCenter(uv4, _MainTex_TexelSize);
        }

        float weight0 = 0.2270270270 * LensBlurTapWeight(centerOwner, uv0);
        float weight1 = 0.3162162162 * LensBlurTapWeight(centerOwner, uv1);
        float weight2 = 0.3162162162 * LensBlurTapWeight(centerOwner, uv2);
        float weight3 = 0.0702702703 * LensBlurTapWeight(centerOwner, uv3);
        float weight4 = 0.0702702703 * LensBlurTapWeight(centerOwner, uv4);

        float4 result = tex2D(
            _MainTex,
            OrientedUV(uv0, _MainTex_TexelSize)) * weight0;
        result += tex2D(
            _MainTex,
            OrientedUV(uv1, _MainTex_TexelSize)) * weight1;
        result += tex2D(
            _MainTex,
            OrientedUV(uv2, _MainTex_TexelSize)) * weight2;
        result += tex2D(
            _MainTex,
            OrientedUV(uv3, _MainTex_TexelSize)) * weight3;
        result += tex2D(
            _MainTex,
            OrientedUV(uv4, _MainTex_TexelSize)) * weight4;
        float totalWeight =
            weight0 + weight1 + weight2 + weight3 + weight4;
        if (_BlurDomainIsolation > 0.5 &&
            _BlurPackFusionChannels < 0.5 &&
            totalWeight <= 0.00001)
        {
            float2 centerUV = clamp(i.uv, 0.0, 1.0);
            result.rgb = tex2D(
                _MainTex,
                OrientedUV(centerUV, _MainTex_TexelSize)).rgb;
        }
        else
        {
            result /= max(totalWeight, 0.00001);
        }

        if (_BlurDomainIsolation > 0.5)
        {
            if (_BlurPackFusionChannels > 0.5)
                result *= LensFusionChannelMask(centerOwner);
            else
                // Encode groups 0..3 as 0.2..0.8 so ARGB32 does not clamp them.
                result.a =
                    (LensFusionGroupAt(centerOwner) + 1.0) * 0.2;
        }
        return result;
    }

    void AccumulateBloomPrefilterTap(
        float2 sampleUV,
        float baseWeight,
        float centerOwner,
        inout float3 color,
        inout float totalWeight)
    {
        sampleUV = clamp(sampleUV, 0.0, 1.0);
        float weight = baseWeight *
            LensTopologyTapWeight(centerOwner, sampleUV);
        color += SampleSource(sampleUV).rgb * weight;
        totalWeight += weight;
    }

    float4 FragBloomPrefilter(v2f i) : SV_Target
    {
        float2 footprint = abs(_MainTex_TexelSize.xy) *
            _SourceToTargetScale * 0.32;
        float centerOwner = LensOwnerOnly(i.uv);
        if (centerOwner < -0.5)
            return float4(0.0, 0.0, 0.0, 0.0);

        float3 color = float3(0.0, 0.0, 0.0);
        float totalWeight = 0.0;
        AccumulateBloomPrefilterTap(
            i.uv, 4.0, centerOwner, color, totalWeight);
        AccumulateBloomPrefilterTap(
            i.uv + float2( footprint.x, 0.0),
            2.0, centerOwner, color, totalWeight);
        AccumulateBloomPrefilterTap(
            i.uv + float2(-footprint.x, 0.0),
            2.0, centerOwner, color, totalWeight);
        AccumulateBloomPrefilterTap(
            i.uv + float2(0.0,  footprint.y),
            2.0, centerOwner, color, totalWeight);
        AccumulateBloomPrefilterTap(
            i.uv + float2(0.0, -footprint.y),
            2.0, centerOwner, color, totalWeight);
        AccumulateBloomPrefilterTap(
            i.uv + float2( footprint.x,  footprint.y),
            1.0, centerOwner, color, totalWeight);
        AccumulateBloomPrefilterTap(
            i.uv + float2(-footprint.x,  footprint.y),
            1.0, centerOwner, color, totalWeight);
        AccumulateBloomPrefilterTap(
            i.uv + float2( footprint.x, -footprint.y),
            1.0, centerOwner, color, totalWeight);
        AccumulateBloomPrefilterTap(
            i.uv + float2(-footprint.x, -footprint.y),
            1.0, centerOwner, color, totalWeight);

        color /= max(totalWeight, 0.00001);
        color = BloomPrefilter(color) * EffectLensMask(i.uv);
        float bloomSignal = Luminance(color);
        return LensFusionChannelMask(centerOwner) * bloomSignal;
    }

    float ExposureWeight(float2 uv)
    {
        float optic = EffectLensMask(uv);
        if (optic <= 0.0001)
            return 0.0;
        float2 opticUV = OpticTextureUV(uv);
        float domainValid = step(-0.5, LensOwnerAtOpticUV(opticUV));
        return optic * domainValid;
    }

    float4 FragLuminancePrefilter(v2f i) : SV_Target
    {
        float2 footprint = abs(_MainTex_TexelSize.xy) *
            _SourceToTargetScale * 0.32;
        float3 color = SampleSource(i.uv).rgb * 4.0;
        color += SampleSource(
            i.uv + float2( footprint.x, 0.0)).rgb * 2.0;
        color += SampleSource(
            i.uv + float2(-footprint.x, 0.0)).rgb * 2.0;
        color += SampleSource(
            i.uv + float2(0.0,  footprint.y)).rgb * 2.0;
        color += SampleSource(
            i.uv + float2(0.0, -footprint.y)).rgb * 2.0;
        color += SampleSource(
            i.uv + float2( footprint.x,  footprint.y)).rgb;
        color += SampleSource(
            i.uv + float2(-footprint.x,  footprint.y)).rgb;
        color += SampleSource(
            i.uv + float2( footprint.x, -footprint.y)).rgb;
        color += SampleSource(
            i.uv + float2(-footprint.x, -footprint.y)).rgb;
        color *= 1.0 / 16.0;

        float weight = ExposureWeight(i.uv);
        float luminance = Luminance(color);
        float logLuminance = log2(max(luminance, 0.0001));
        return float4(
            logLuminance * weight,
            weight,
            luminance * weight,
            1.0);
    }

    float4 FragLuminanceDownsample(v2f i) : SV_Target
    {
        float2 texel = abs(_MainTex_TexelSize.xy) * 0.5;
        float2 uv = OrientedUV(i.uv, _MainTex_TexelSize);
        float4 result = tex2D(
            _MainTex, uv + float2( texel.x,  texel.y));
        result += tex2D(
            _MainTex, uv + float2(-texel.x,  texel.y));
        result += tex2D(
            _MainTex, uv + float2( texel.x, -texel.y));
        result += tex2D(
            _MainTex, uv + float2(-texel.x, -texel.y));
        return result * 0.25;
    }

    float4 FragExposureAdapt(v2f i) : SV_Target
    {
        float3 accumulated = tex2D(
            _MainTex,
            OrientedUV(float2(0.5, 0.5), _MainTex_TexelSize)).rgb;
        float geometricLuminance = exp2(
            accumulated.x / max(accumulated.y, 0.0001));
        float arithmeticLuminance = accumulated.z /
            max(accumulated.y, 0.0001);
        float averageLuminance = lerp(
            geometricLuminance,
            arithmeticLuminance,
            saturate(_HighlightProtection));
        float targetEV = log2(
            max(_ExposureTarget, 0.0001) /
            max(averageLuminance, 0.0001));
        targetEV = clamp(
            targetEV,
            _ExposureEVMinMax.x,
            _ExposureEVMinMax.y);

        float previousEV = tex2D(
            _ExposureHistory,
            float2(0.5, 0.5)).r;
        if (_ExposureHistoryValid < 0.5)
            previousEV = targetEV;

        float speed = targetEV < previousEV
            ? _AdaptationSpeeds.x
            : _AdaptationSpeeds.y;
        float blend = 1.0 - exp(
            -max(speed, 0.0) * max(_DeltaTime, 0.0));
        float adaptedEV = lerp(
            previousEV,
            targetEV,
            saturate(blend));
        return float4(
            adaptedEV,
            averageLuminance,
            exp2(adaptedEV),
            1.0);
    }

    float4 FragComposite(v2f i) : SV_Target
    {
        float4 original = SampleSource(i.uv);
        float lensMask = EffectLensMask(i.uv);

        // The scene remains untouched outside the texture-defined aperture.
        if (lensMask <= 0.0001)
        {
            float3 outsideOnly = ApplyMaskOverlay(original.rgb, i.uv);
            return float4(outsideOnly, original.a);
        }

        LensDomainData lensDomain;
        float2 warpedUV = DistortedUV(i.uv, lensDomain);
        float3 sharpScene = SampleSource(warpedUV).rgb;
        float3 opticalScene = sharpScene;

        if (_OpticalHazeEnabled > 0.5)
        {
            float edgeWeight = 0.0;
            if (lensDomain.valid > 0.5)
            {
                float edgeDistancePixels = OpticDistanceToPixels(
                    lensDomain.outerEdgeDistance);
                float edgeBand = 1.0 - smoothstep(
                    0.0,
                    max(_OpticalHazeEdgeWidthPixels, 0.00001),
                    edgeDistancePixels);
                edgeWeight = pow(
                    saturate(edgeBand),
                    _OpticalHazeFalloff);
            }

            float3 hazeColor = SampleOpticalHaze(warpedUV, sharpScene);
            float hazeStrength = lerp(
                _OpticalHazeCenterStrength,
                _OpticalHazeEdgeStrength,
                edgeWeight);
            opticalScene = lerp(sharpScene, hazeColor, hazeStrength);
            opticalScene += hazeColor *
                (_OpticalHazeVeilStrength * edgeWeight);

            if (lensDomain.valid > 0.5 &&
                edgeWeight > 0.00001 &&
                abs(_ChromaticAberrationPixels) > 0.00001)
            {
                float screenAspect = _SourceSize.x /
                    max(_SourceSize.y, 1.0);
                float2 radialScreenNormal = float2(
                    lensDomain.outwardNormal.x /
                        max(screenAspect, 0.0001),
                    lensDomain.outwardNormal.y);
                float2 chromaticOffset = radialScreenNormal *
                    (_ChromaticAberrationPixels /
                        max(_SourceSize.y, 1.0));
                float centerOwner = LensOwnerOnly(warpedUV);
                float2 redUV = clamp(
                    warpedUV + chromaticOffset,
                    0.0,
                    1.0);
                float2 blueUV = clamp(
                    warpedUV - chromaticOffset,
                    0.0,
                    1.0);
                float2 chromaticGuardOffset = radialScreenNormal *
                    (sign(_ChromaticAberrationPixels) * 0.75 /
                        max(_SourceSize.y, 1.0));
                float chromaticCenterValid;
                float redValid;
                float blueValid;
                float redDomainValid = centerOwner > -0.5
                    ? LensTopologyTapWeight(centerOwner, redUV) *
                        LensTopologyTapWeight(
                            centerOwner,
                            redUV + chromaticGuardOffset)
                    : 0.0;
                float blueDomainValid = centerOwner > -0.5
                    ? LensTopologyTapWeight(centerOwner, blueUV) *
                        LensTopologyTapWeight(
                            centerOwner,
                            blueUV - chromaticGuardOffset)
                    : 0.0;
                float3 chromaticCenter = SampleFusionSafeSource(
                    warpedUV,
                    centerOwner,
                    sharpScene,
                    chromaticCenterValid);
                float red = SampleFusionSafeSource(
                    redUV,
                    centerOwner,
                    chromaticCenter,
                    redValid).r;
                float blue = SampleFusionSafeSource(
                    blueUV,
                    centerOwner,
                    chromaticCenter,
                    blueValid).b;
                float3 chromaticDelta = float3(
                    (red - chromaticCenter.r) *
                        (redValid * redDomainValid),
                    0.0,
                    (blue - chromaticCenter.b) *
                        (blueValid * blueDomainValid));
                opticalScene += chromaticDelta *
                    (edgeWeight * chromaticCenterValid);
            }
        }

        if (_NearBlurEnabled > 0.5)
        {
            float centerCoC = NearCoC(warpedUV);
            float4 nearSample = tex2D(_NearBlurTex, warpedUV);
            float3 nearColor = nearSample.rgb /
                max(nearSample.a, 0.0001);
            float blurBlend = max(
                centerCoC,
                nearSample.a * _ForegroundExpansion);
            blurBlend = saturate(blurBlend * _NearBlurStrength);
            opticalScene = lerp(opticalScene, nearColor, blurBlend);
        }

        float exposureEV = tex2D(
            _ExposureTex,
            float2(0.5, 0.5)).r;
        exposureEV = lerp(
            0.0,
            exposureEV,
            saturate(_AutoExposure));
        float totalGain = max(_ManualGain, 0.0) * exp2(exposureEV);

        // Keep raw scene light independent from gain, bloom and tube output.
        // It is also the sole input to the local noise-visibility mask.
        float rawSceneLuminance = max(Luminance(opticalScene), 0.0);
        float signal = rawSceneLuminance * totalGain;

        if (_BloomEnabled > 0.5)
        {
            float bloom = BloomSignalFromSample(
                tex2D(_BloomTex, warpedUV),
                lensDomain.ownerIndex);
            bloom += BloomSignalFromSample(
                tex2D(_BloomWideTex, warpedUV),
                lensDomain.ownerIndex) * 0.67;
            signal += bloom * totalGain * _BloomIntensity;
        }

        // Preserve over-range tube energy until after phosphor coloration.
        // Clipping each RGB channel at the end lets intense highlights converge
        // toward white, matching the response of the old shader.
        float tube = max(signal, 0.0);

        // Local input light decides where noise is visible. Gain independently
        // controls how strongly the tube amplifies the remaining noise.
        float noiseFadeStart = max(_NoiseFadeStartLuminance, 0.0);
        float noiseFadeEnd = max(
            _NoiseFadeEndLuminance,
            noiseFadeStart + 0.0001);
        float localNoiseVisibility = 1.0 - smoothstep(
            noiseFadeStart,
            noiseFadeEnd,
            rawSceneLuminance);
        float effectiveGainEV = log2(max(totalGain, 0.0001));
        float gainLevel = saturate(
            (effectiveGainEV - _ExposureEVMinMax.x) /
            max(_ExposureEVMinMax.y - _ExposureEVMinMax.x, 0.0001));
        float gainNoiseBoost = lerp(0.65, 1.65, gainLevel);

        float2 pixel = i.uv * _SourceSize.xy;
        float grainSize = max(_GrainPixelSize, 0.5);
        float frame = fmod(
            floor(_EffectTime * max(_NoiseRefreshRate, 1.0)),
            4096.0);
        float2 fineCell = floor(pixel / grainSize);
        float2 coarseCell = floor(pixel / (grainSize * 3.0));
        float fineNoise = GaussianHash(fineCell, frame);
        float coarseNoise = GaussianHash(
            coarseCell,
            floor(frame * 0.53) + 137.0);
        float fixedPattern = (
            ValueNoise(pixel / 48.0 + 17.1) - 0.5) * 2.0;
        fixedPattern += (
            Hash12(floor(pixel / 5.0) + 91.7) - 0.5) * 0.45;

        float fixedPatternVisibility = lerp(0.7, 1.4, gainLevel);
        tube *= 1.0 + fixedPattern * _FixedPatternNoise *
            fixedPatternVisibility * localNoiseVisibility;
        float sigma = _ReadNoise +
            _ShotNoise * sqrt(max(tube, 0.001));
        sigma *= gainNoiseBoost;
        float noise = fineNoise * sigma;
        noise += coarseNoise * _CoarseNoise *
            gainNoiseBoost;
        noise *= localNoiseVisibility;

        float scintillationVisibility = saturate(
            0.15 + gainLevel * 0.85);
        float baseEventProbability = lerp(
            0.0005,
            0.0020,
            scintillationVisibility);

        // One possible cluster is generated per 3x3-pixel tile. Dividing by
        // the average two-pixel cluster size keeps density 1 close to the old
        // per-pixel event count while allowing connected one-to-three-pixel
        // shapes without a spatial trajectory.
        float2 scintillationPixel = floor(pixel);
        float2 scintillationTile = floor(scintillationPixel / 3.0);
        float2 pixelInTile = scintillationPixel - scintillationTile * 3.0;
        float tileEventProbability = saturate(
            baseEventProbability * 4.5 * max(_ScintillationDensity, 0.0));
        tileEventProbability *= step(0.0001, localNoiseVisibility);
        float tileEventHash = Hash13(float3(scintillationTile, frame));
        float clusterExists = step(
            1.0 - tileEventProbability,
            tileEventHash);

        float clusterPixel = 0.0;
        if (clusterExists > 0.5)
        {
        float anchorXHash = Hash13(float3(
            scintillationTile + 17.3,
            frame + 131.0));
        float anchorYHash = Hash13(float3(
            scintillationTile.yx + 43.7,
            frame + 277.0));
        float2 anchorPixel = floor(
            float2(anchorXHash, anchorYHash) * 3.0);

        float directionHash = Hash13(float3(
            scintillationTile + 89.1,
            frame + 419.0));
        float2 firstDirection = CardinalDirection(directionHash);
        firstDirection = KeepStepInsideTile(anchorPixel, firstDirection);
        float2 secondPixel = anchorPixel + firstDirection;

        float shapeHash = Hash13(float3(
            scintillationTile.yx + 157.3,
            frame + 613.0));
        float2 secondDirection = shapeHash < 0.5
            ? float2(-firstDirection.y, firstDirection.x)
            : float2(firstDirection.y, -firstDirection.x);

        float2 thirdCandidate = secondPixel + secondDirection;
        bool thirdOutside =
            thirdCandidate.x < 0.0 || thirdCandidate.x > 2.0 ||
            thirdCandidate.y < 0.0 || thirdCandidate.y > 2.0;
        if (thirdOutside)
        {
            secondDirection = -secondDirection;
            thirdCandidate = secondPixel + secondDirection;
        }

        float sizeHash = Hash13(float3(
            scintillationTile + 223.9,
            frame + 811.0));
        float clusterSize = 1.0 + floor(sizeHash * 3.0);
        clusterPixel = SamePixel(pixelInTile, anchorPixel);
        clusterPixel = max(
            clusterPixel,
            step(1.5, clusterSize) * SamePixel(pixelInTile, secondPixel));
        clusterPixel = max(
            clusterPixel,
            step(2.5, clusterSize) * SamePixel(pixelInTile, thirdCandidate));
        }

        float scintillationAmplitude = _Scintillation *
            lerp(0.2, 1.4, scintillationVisibility) *
            (1.25 - saturate(tube)) *
            localNoiseVisibility;
        noise += clusterExists * clusterPixel * scintillationAmplitude;
        tube = max(tube + noise, 0.0);

        float transmission = 1.0;
        if (_MultiLensVignetteEnabled > 0.5 &&
            lensDomain.valid > 0.5)
        {
            float outerVignette = 1.0 - smoothstep(
                0.0,
                max(_MultiLensOuterVignetteWidth, 0.00001),
                lensDomain.outerEdgeDistance);
            outerVignette = pow(
                saturate(outerVignette),
                max(_MultiLensVignetteFalloff, 0.01));

            float outerLoss = saturate(
                _MultiLensOuterVignetteStrength *
                max(lensDomain.outerVignetteMultiplier, 0.0) *
                outerVignette);
            transmission *= 1.0 - outerLoss;
        }
        tube *= saturate(transmission);

        float3 nightVision = saturate(tube * _PhosphorTint.rgb);

        if (_LensSeamMode > 0.5 && lensDomain.valid > 0.5)
        {
            float seamComposite = 0.0;
            if (_LensSeamMode < 1.5)
                seamComposite = lensDomain.seamWeight * 0.35;
            else if (_LensSeamMode < 2.5)
                seamComposite = lensDomain.seamWeight;
            else
                seamComposite = step(0.5, lensDomain.seamWeight);
            seamComposite *= saturate(_LensSeamOpacity);
            nightVision = lerp(
                nightVision,
                _LensSeamColor.rgb,
                saturate(seamComposite));
        }

        float3 result = lerp(original.rgb, nightVision, lensMask);
        result = ApplyMaskOverlay(result, i.uv);
        return float4(result, original.a);
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Pass numbers are part of the controller contract. Keep this order.
        Pass
        {
            Name "FINAL_COMPOSITE"
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment FragComposite
            ENDCG
        }

        Pass
        {
            Name "NEAR_DOF_PREPARE"
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment FragNearPrepare
            ENDCG
        }

        Pass
        {
            Name "GAUSSIAN_BLUR"
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment FragGaussian
            ENDCG
        }

        Pass
        {
            Name "BLOOM_PREFILTER"
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment FragBloomPrefilter
            ENDCG
        }

        Pass
        {
            Name "LUMINANCE_PREFILTER"
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment FragLuminancePrefilter
            ENDCG
        }

        Pass
        {
            Name "LUMINANCE_DOWNSAMPLE"
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment FragLuminanceDownsample
            ENDCG
        }

        Pass
        {
            Name "EXPOSURE_ADAPT"
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment FragExposureAdapt
            ENDCG
        }
    }

    Fallback Off
}
