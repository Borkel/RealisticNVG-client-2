using UnityEngine;
using BorkelRNVG.Models;

namespace BorkelRNVG.Controllers
{
[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public sealed class RealisticNightVisionRenderer : MonoBehaviour
{
    public enum LensLayoutPreset
    {
        Pvs14 = 1,
        Gpnvg = 2,
        DualTube = 4,
        Custom = 3
    }

    public enum LensSeamMode
    {
        None = 0,
        Soft = 1,
        Dark = 2,
        Hard = 3
    }

    [System.Serializable]
    public struct LensDefinition
    {
        public bool enabled;

        public Vector2 centerUV;

        public float radiusInTextureHeights;
        public float distortionMultiplier;

        public int fusionGroup;

        public float vignetteMultiplier;

        public LensDefinition(Vector2 center, float radius, float multiplier,
            int group = 0, float vignette = 1f)
        {
            enabled = true;
            centerUV = center;
            radiusInTextureHeights = radius;
            distortionMultiplier = multiplier;
            fusionGroup = group;
            vignetteMultiplier = vignette;
        }
    }

    private const string ShaderName = "Hidden/CustomNightVision";

    private const int PassComposite = 0;
    private const int PassNearPrepare = 1;
    private const int PassGaussian = 2;
    private const int PassBloomPrefilter = 3;
    private const int PassLuminancePrefilter = 4;
    private const int PassLuminanceDownsample = 5;
    private const int PassExposureAdapt = 6;
    private const int MaximumLensDefinitions = 4;
    private const float GaussianKernelExtent = 3.23076923f;

    [SerializeField] private Shader effectShader;
    [SerializeField] private bool nightVisionEnabled = true;

    [SerializeField] private Color phosphorTint = new Color(0.62f, 0.98f, 0.92f, 1f);

    [SerializeField] private Vector3 spectralSensitivity = new Vector3(0.18f, 0.72f, 0.10f);
    [SerializeField] private float manualGain = 1.35f;

    [SerializeField] private bool autoExposure = true;
    [SerializeField] private float targetLuminance = 0.18f;
    [SerializeField] private float minimumExposureEV = -2f;
    [SerializeField] private float maximumExposureEV = 5f;

    [SerializeField] private float brightSceneSpeed = 10f;

    [SerializeField] private float darkSceneSpeed = 2f;

    [SerializeField] private float highlightProtection = 0.35f;

    [SerializeField] private bool nearDepthOfField = true;

    [SerializeField] private float fullBlurDistance = 0.35f;

    [SerializeField] private float sharpDistance = 4f;
    [SerializeField] private float nearBlurStrength = 1f;
    [SerializeField] private float nearBlurRadiusPixels = 16f;
    [SerializeField] private int nearBlurDownsample = 2;
    [SerializeField] private int nearBlurIterations = 2;

    [SerializeField] private float foregroundExpansion = 1.15f;

    [SerializeField] private bool opticalHazeEnabled;
    [SerializeField] private float opticalHazeBlurRadiusPixels = 6f;
    [SerializeField] private float opticalHazeCenterStrength = 0.1f;
    [SerializeField] private float opticalHazeEdgeStrength = 0.8f;
    [SerializeField] private float opticalHazeEdgeWidthPixels = 90f;
    [SerializeField] private float opticalHazeFalloff = 1.5f;
    [SerializeField] private float opticalHazeVeilStrength = 0.08f;
    [SerializeField] private float opticalHazeChromaticAberrationPixels = 1.25f;
    [SerializeField] private int opticalHazeDownsample = 2;
    [SerializeField] private int opticalHazeIterations = 1;

    [SerializeField] private bool bloom = true;

    [SerializeField] private float bloomThreshold = 0f;
    [SerializeField] private float bloomSoftKnee = 0.5f;
    [SerializeField] private float bloomIntensity = 0.13f;
    [SerializeField] private float bloomRadiusPixels = 22f;
    [SerializeField] private int bloomDownsample = 4;
    [SerializeField] private int bloomIterations = 2;
    [SerializeField] private float wideBloomRadiusPixels = 80f;

    [SerializeField] private float readNoise = 0.03f;
    [SerializeField] private float shotNoise = 0.065f;
    [SerializeField] private float coarseNoise = 0.022f;
    [SerializeField] private float fixedPatternNoise = 0.018f;
    [SerializeField] private float scintillation = 0.04f;
    [Tooltip("Multiplier for the number of scintillation events. Zero disables them.")]
    [Min(0f)]
    [SerializeField] private float scintillationDensity = 1f;
    [SerializeField] private float noiseFadeStartLuminance = 0.018f;
    [SerializeField] private float noiseFadeEndLuminance = 0.18f;
    [SerializeField] private float grainPixelSize = 1f;
    [SerializeField] private float noiseRefreshRate = 60f;

    [SerializeField] private Vector2 opticTextureCenter = new Vector2(0.5f, 0.5f);
    [SerializeField] private Texture2D lensTexture;
    [SerializeField] private Texture2D maskOverlay;
    [SerializeField] private float lensAlphaCutoff = 0.55f;
    [SerializeField] private float lensAlphaFeather = 0.005f;
    [SerializeField] private float opticTextureScale = 1f;

    [SerializeField] private LensLayoutPreset lensLayoutPreset = LensLayoutPreset.Pvs14;
    [SerializeField] private LensDefinition[] lensDefinitions = new LensDefinition[MaximumLensDefinitions];

    [SerializeField] private bool multiLensEdgeDistortion = true;

    [SerializeField] private float multiLensDistortionStrengthPixels = 6f;

    [SerializeField] private float multiLensDistortionWidthPixels = 60f;
    [SerializeField] private float multiLensDistortionFalloff = 2f;

    [SerializeField] private LensSeamMode lensSeamMode = LensSeamMode.Dark;

    [SerializeField] private float lensSeamWidth = 0.006f;
    [SerializeField] private float lensSeamOpacity = 0.65f;
    [SerializeField] private Color lensSeamColor = Color.black;

    [SerializeField] private bool multiLensVignette = true;

    [SerializeField] private float multiLensOuterVignetteWidth = 0.12f;
    [SerializeField] private float multiLensOuterVignetteStrength = 0.68f;
    [SerializeField] private float multiLensVignetteFalloff = 1.35f;

    private Camera targetCamera;
    private SSAAPropagator ssaaPropagator;
    private Material material;
    private readonly RenderTexture[] exposureRead =
        new RenderTexture[MaximumLensDefinitions];
    private readonly RenderTexture[] exposureWrite =
        new RenderTexture[MaximumLensDefinitions];
    private RenderTextureFormat intermediateFormat;
    private bool supportsSignedIntermediates;
    private bool exposureHistoryValid;
    private bool shaderErrorReported;
    private bool missingOpticTexturesReported;
    private bool depthModeAdded;
    private float previousRenderTime = -1f;

    private static readonly int SourceSizeId = Shader.PropertyToID("_SourceSize");
    private static readonly int SourceToTargetScaleId = Shader.PropertyToID("_SourceToTargetScale");
    private static readonly int BlurDirectionId = Shader.PropertyToID("_BlurDirection");
    private static readonly int BlurDomainIsolationId = Shader.PropertyToID("_BlurDomainIsolation");
    private static readonly int BlurPackFusionChannelsId = Shader.PropertyToID("_BlurPackFusionChannels");
    private static readonly int OpticalHazeTexId = Shader.PropertyToID("_OpticalHazeTex");
    private static readonly int OpticalHazeTextureAvailableId =
        Shader.PropertyToID("_OpticalHazeTextureAvailable");
    private static readonly int NearBlurTexId = Shader.PropertyToID("_NearBlurTex");
    private static readonly int BloomTexId = Shader.PropertyToID("_BloomTex");
    private static readonly int BloomWideTexId = Shader.PropertyToID("_BloomWideTex");
    private static readonly int[] ExposureTexIds =
    {
        Shader.PropertyToID("_ExposureTex0"),
        Shader.PropertyToID("_ExposureTex1"),
        Shader.PropertyToID("_ExposureTex2"),
        Shader.PropertyToID("_ExposureTex3")
    };
    private static readonly int ExposureHistoryId = Shader.PropertyToID("_ExposureHistory");
    private static readonly int ExposureLensIndexId =
        Shader.PropertyToID("_ExposureLensIndex");
    private readonly Vector4[] lensDefinitionUpload = new Vector4[MaximumLensDefinitions];

    private bool NearPassActive
    {
        get
        {
            return OpticReady && nightVisionEnabled && nearDepthOfField && nearBlurStrength > 0f &&
                   nearBlurRadiusPixels > 0f;
        }
    }

    private bool BloomPassActive
    {
        get { return nightVisionEnabled && bloom && bloomIntensity > 0f; }
    }

    private bool OpticalHazeTextureRequired
    {
        get
        {
            return nightVisionEnabled && opticalHazeEnabled &&
                   opticalHazeBlurRadiusPixels != 0f &&
                   (opticalHazeCenterStrength != 0f ||
                    opticalHazeEdgeStrength != 0f ||
                    opticalHazeVeilStrength != 0f);
        }
    }

    private bool AutoExposureActive
    {
        get { return OpticReady && nightVisionEnabled && autoExposure && supportsSignedIntermediates; }
    }

    private bool OpticReady
    {
        get { return lensTexture != null && maskOverlay != null; }
    }

    public bool NightVisionEnabled
    {
        get { return nightVisionEnabled; }
        set
        {
            if (nightVisionEnabled == value)
                return;
            nightVisionEnabled = value;
            ResetExposure();
            UpdateDepthTextureMode();
        }
    }

    public float ManualGain
    {
        get { return manualGain; }
        set { manualGain = value; }
    }

    public void ConfigureRuntime(Shader shader, Texture lens, Texture overlay,
        RealisticNvgSettings settings, SSAAPropagator propagator,
        float globalGain, float globalScale)
    {
        if (settings == null)
            settings = new RealisticNvgSettings();

        effectShader = shader;
        ssaaPropagator = propagator;
        lensTexture = lens as Texture2D;
        maskOverlay = overlay as Texture2D;

        phosphorTint = new Color(settings.PhosphorRed,
            settings.PhosphorGreen, settings.PhosphorBlue, 1f);
        spectralSensitivity = new Vector3(settings.SpectralSensitivityRed,
            settings.SpectralSensitivityGreen, settings.SpectralSensitivityBlue);
        manualGain = settings.ManualGain * globalGain;

        autoExposure = settings.AutoExposure;
        targetLuminance = settings.TargetLuminance;
        minimumExposureEV = settings.MinimumExposureEV;
        maximumExposureEV = settings.MaximumExposureEV;
        brightSceneSpeed = settings.BrightSceneSpeed;
        darkSceneSpeed = settings.DarkSceneSpeed;
        highlightProtection = settings.HighlightProtection;

        nearDepthOfField = settings.NearDepthOfField;
        fullBlurDistance = settings.FullBlurDistance;
        sharpDistance = settings.SharpDistance;
        nearBlurStrength = settings.NearBlurStrength;
        nearBlurRadiusPixels = settings.NearBlurRadiusPixels;

        opticalHazeEnabled = settings.OpticalHaze;
        opticalHazeBlurRadiusPixels = settings.HazeBlurRadiusPixels;
        opticalHazeCenterStrength = settings.HazeCenterStrength;
        opticalHazeEdgeStrength = settings.HazeEdgeStrength;
        opticalHazeEdgeWidthPixels = settings.HazeEdgeWidthPixels;
        opticalHazeFalloff = settings.HazeFalloff;
        opticalHazeVeilStrength = settings.HazeVeilStrength;
        opticalHazeChromaticAberrationPixels = settings.ChromaticAberrationPixels;

        bloom = settings.Bloom;
        bloomThreshold = settings.BloomThreshold;
        bloomSoftKnee = settings.BloomSoftKnee;
        bloomIntensity = settings.BloomIntensity;
        bloomRadiusPixels = settings.BloomRadiusPixels;
        wideBloomRadiusPixels = settings.WideBloomRadiusPixels;

        readNoise = settings.ReadNoise;
        shotNoise = settings.ShotNoise;
        coarseNoise = settings.CoarseNoise;
        fixedPatternNoise = settings.FixedPatternNoise;
        scintillation = settings.Scintillation;
        scintillationDensity = settings.ScintillationDensity;
        noiseFadeStartLuminance = settings.NoiseFadeStartLuminance;
        noiseFadeEndLuminance = settings.NoiseFadeEndLuminance;
        grainPixelSize = settings.GrainPixelSize;
        noiseRefreshRate = settings.NoiseRefreshRate;

        opticTextureScale = settings.OpticScale * globalScale;
        switch (settings.LensLayout)
        {
            case NvgLensLayout.DualTube:
                lensLayoutPreset = LensLayoutPreset.DualTube;
                break;
            case NvgLensLayout.Gpnvg:
                lensLayoutPreset = LensLayoutPreset.Gpnvg;
                break;
            default:
                lensLayoutPreset = LensLayoutPreset.Pvs14;
                break;
        }
        lensSeamMode = lensLayoutPreset == LensLayoutPreset.Gpnvg
            ? LensSeamMode.Dark
            : LensSeamMode.None;
        multiLensEdgeDistortion = settings.EdgeDistortion;
        multiLensDistortionStrengthPixels = settings.DistortionStrengthPixels;
        multiLensDistortionWidthPixels = settings.DistortionWidthPixels;
        multiLensDistortionFalloff = settings.DistortionFalloff;
        lensSeamWidth = settings.SeamWidth;
        lensSeamOpacity = settings.SeamOpacity;
        multiLensVignette = settings.Vignette;
        multiLensOuterVignetteWidth = settings.VignetteWidth;
        multiLensOuterVignetteStrength = settings.VignetteStrength;
        multiLensVignetteFalloff = settings.VignetteFalloff;

        ApplyBuiltInPreset(lensLayoutPreset);
        ResetExposure();
        UpdateDepthTextureMode();
        EnsureMaterial();
    }

    public void ConfigureOptic(Texture2D lens, Texture2D overlay,
        LensLayoutPreset preset, LensSeamMode seamMode, float scale = 1f)
    {
        lensTexture = lens;
        maskOverlay = overlay;
        opticTextureScale = scale;
        lensSeamMode = IsValidSeamMode(seamMode) ? seamMode : LensSeamMode.Dark;
        lensLayoutPreset = IsValidPreset(preset) ? preset : LensLayoutPreset.Pvs14;
        ApplyBuiltInPreset(lensLayoutPreset);
        ResetExposure();
        UpdateDepthTextureMode();
    }

    public void ConfigureLensEdgeDistortion(bool enabled, float strengthPixels,
        float widthPixels, float falloff)
    {
        multiLensEdgeDistortion = enabled;
        multiLensDistortionStrengthPixels = strengthPixels;
        multiLensDistortionWidthPixels = widthPixels;
        multiLensDistortionFalloff = falloff;
    }

    public void ConfigureOpticalHaze(bool enabled, float blurRadiusPixels,
        float centerStrength, float edgeStrength, float edgeWidthPixels,
        float falloff, float veilStrength, float chromaticAberrationPixels,
        int downsample, int iterations)
    {
        opticalHazeEnabled = enabled;
        opticalHazeBlurRadiusPixels = blurRadiusPixels;
        opticalHazeCenterStrength = centerStrength;
        opticalHazeEdgeStrength = edgeStrength;
        opticalHazeEdgeWidthPixels = edgeWidthPixels;
        opticalHazeFalloff = falloff;
        opticalHazeVeilStrength = veilStrength;
        opticalHazeChromaticAberrationPixels = chromaticAberrationPixels;
        opticalHazeDownsample = downsample;
        opticalHazeIterations = iterations;
        ResetExposure();
    }

    public void ConfigureMultiLensVignette(bool enabled, float outerWidth,
        float outerStrength, float falloff)
    {
        multiLensVignette = enabled;
        multiLensOuterVignetteWidth = outerWidth;
        multiLensOuterVignetteStrength = outerStrength;
        multiLensVignetteFalloff = falloff;
    }

    public void SetLensDefinition(int index, LensDefinition definition)
    {
        EnsureLensDefinitionArray();
        if (index < 0 || index >= MaximumLensDefinitions)
            throw new System.ArgumentOutOfRangeException("index");

        lensDefinitions[index] = definition;
        lensLayoutPreset = LensLayoutPreset.Custom;
        ResetExposure();
    }

    public void SetLensDefinitions(LensDefinition[] definitions)
    {
        EnsureLensDefinitionArray();
        for (int index = 0; index < MaximumLensDefinitions; index++)
        {
            LensDefinition definition = definitions != null && index < definitions.Length
                ? definitions[index]
                : default(LensDefinition);
            lensDefinitions[index] = definition;
        }
        lensLayoutPreset = LensLayoutPreset.Custom;
        ResetExposure();
    }

    public void ResetExposure()
    {
        exposureHistoryValid = false;
        previousRenderTime = -1f;
    }

    private void OnEnable()
    {
        EnsureLensDefinitionArray();
        ApplyBuiltInPreset(lensLayoutPreset);
        targetCamera = GetComponent<Camera>();
        if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
        {
            intermediateFormat = RenderTextureFormat.ARGBHalf;
            supportsSignedIntermediates = true;
        }
        else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
        {
            intermediateFormat = RenderTextureFormat.ARGBFloat;
            supportsSignedIntermediates = true;
        }
        else
        {
            intermediateFormat = RenderTextureFormat.ARGB32;
            supportsSignedIntermediates = false;
        }
        ResetExposure();
        UpdateDepthTextureMode();
        EnsureMaterial();
    }

    private void OnValidate()
    {
        EnsureLensDefinitionArray();
        ApplyBuiltInPreset(lensLayoutPreset);
        ResetExposure();

        if (!isActiveAndEnabled)
            return;

        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
        UpdateDepthTextureMode();
    }

    private void OnPreCull()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
        UpdateDepthTextureMode();
    }

    private void UpdateDepthTextureMode()
    {
        if (targetCamera == null)
            return;

        if (isActiveAndEnabled && NearPassActive)
        {
            if ((targetCamera.depthTextureMode & DepthTextureMode.Depth) == 0)
            {
                targetCamera.depthTextureMode |= DepthTextureMode.Depth;
                depthModeAdded = true;
            }
        }
        else
        {
            ReleaseDepthTextureMode();
        }
    }

    private void ReleaseDepthTextureMode()
    {
        if (targetCamera == null || !depthModeAdded)
            return;
        targetCamera.depthTextureMode &= ~DepthTextureMode.Depth;
        depthModeAdded = false;
    }

    private void OnDisable()
    {
        ReleaseDepthTextureMode();
        ReleaseExposureBuffers();
        DestroyMaterial();
    }

    private void OnDestroy()
    {
        ReleaseDepthTextureMode();
        ReleaseExposureBuffers();
        DestroyMaterial();
    }

    private bool EnsureMaterial()
    {
        Shader requestedShader = effectShader != null ? effectShader : Shader.Find(ShaderName);
        if (material != null)
        {
            if (material.shader == requestedShader && material.shader != null &&
                material.shader.isSupported && material.passCount >= 7)
                return true;
            DestroyMaterial();
        }

        effectShader = requestedShader;

        if (effectShader == null || !effectShader.isSupported || effectShader.passCount < 7)
        {
            if (!shaderErrorReported)
            {
                Debug.LogError("RealisticNightVisionRenderer needs the supported seven-pass " +
                               ShaderName + " shader from brnvg_shaders.bundle.", this);
                shaderErrorReported = true;
            }
            return false;
        }

        material = new Material(effectShader);
        material.name = "Realistic Night Vision (Runtime)";
        material.hideFlags = HideFlags.HideAndDontSave;
        shaderErrorReported = false;
        return true;
    }

    private void DestroyMaterial()
    {
        if (material == null)
            return;

        if (Application.isPlaying)
            Destroy(material);
        else
            DestroyImmediate(material);
        material = null;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        RenderTexture renderSource = source;
        RenderTexture renderDestination = destination;
        bool borrowedSsaaTargets = false;

        if (ssaaPropagator != null)
        {
            RenderTexture ssaaSource;
            RenderTexture ssaaDestination;
            borrowedSsaaTargets = ssaaPropagator.GetSourceDestination(
                out ssaaSource, out ssaaDestination);
            if (borrowedSsaaTargets)
            {
                renderSource = ssaaSource;
                renderDestination = ssaaDestination;
            }
        }

        try
        {
            RenderEffect(renderSource, renderDestination);
        }
        finally
        {
            if (borrowedSsaaTargets)
                ssaaPropagator.ReleaseSourceDestination(
                    renderSource, renderDestination);
        }
    }

    private void RenderEffect(RenderTexture source, RenderTexture destination)
    {
        if (!nightVisionEnabled)
        {
            Graphics.Blit(source, destination);
            return;
        }

        if (!OpticReady)
        {
            if (!missingOpticTexturesReported)
            {
                Debug.LogWarning("RealisticNightVisionRenderer needs both a lens texture and a mask overlay. The camera image will pass through unchanged.", this);
                missingOpticTexturesReported = true;
            }
            Graphics.Blit(source, destination);
            return;
        }

        if (!EnsureMaterial())
        {
            Graphics.Blit(source, destination);
            return;
        }

        float deltaTime = CalculateDeltaTime();
        SetFrameProperties(source);

        RenderTexture nearA = null;
        RenderTexture nearB = null;
        RenderTexture hazeA = null;
        RenderTexture hazeB = null;
        RenderTexture bloomA = null;
        RenderTexture bloomB = null;
        RenderTexture wideA = null;
        RenderTexture wideB = null;

        try
        {
            if (OpticalHazeTextureRequired)
            {
                int downsample = Mathf.Max(1, opticalHazeDownsample);
                int width = Mathf.Max(1, source.width / downsample);
                int height = Mathf.Max(1, source.height / downsample);
                hazeA = GetTemporary(source, width, height, FilterMode.Bilinear);
                hazeB = GetTemporary(source, width, height, FilterMode.Bilinear);
                BlurFromFullResolutionSource(source, hazeA, hazeB,
                    opticalHazeBlurRadiusPixels,
                    downsample, opticalHazeIterations, true, false);
                material.SetTexture(OpticalHazeTexId, hazeB);
                material.SetFloat(OpticalHazeTextureAvailableId, 1f);
            }
            else
            {
                material.SetTexture(OpticalHazeTexId, Texture2D.blackTexture);
                material.SetFloat(OpticalHazeTextureAvailableId, 0f);
            }

            if (NearPassActive)
            {
                int downsample = Mathf.Max(1, nearBlurDownsample);
                int width = Mathf.Max(1, source.width / downsample);
                int height = Mathf.Max(1, source.height / downsample);
                nearA = GetTemporary(source, width, height, FilterMode.Bilinear);
                nearB = GetTemporary(source, width, height, FilterMode.Bilinear);
                material.SetVector(SourceToTargetScaleId,
                    new Vector4(source.width / (float)width,
                        source.height / (float)height, 0f, 0f));
                Graphics.Blit(source, nearA, material, PassNearPrepare);
                Blur(ref nearA, ref nearB, nearBlurRadiusPixels, downsample,
                    nearBlurIterations, false, false);
                material.SetTexture(NearBlurTexId, nearA);
            }
            else
            {
                material.SetTexture(NearBlurTexId, Texture2D.blackTexture);
            }

            if (BloomPassActive)
            {
                int downsample = Mathf.Max(1, bloomDownsample);
                int width = Mathf.Max(1, source.width / downsample);
                int height = Mathf.Max(1, source.height / downsample);
                bloomA = GetTemporary(source, width, height, FilterMode.Bilinear);
                bloomB = GetTemporary(source, width, height, FilterMode.Bilinear);
                material.SetVector(SourceToTargetScaleId,
                    new Vector4(source.width / (float)width,
                        source.height / (float)height, 0f, 0f));
                Graphics.Blit(source, bloomA, material, PassBloomPrefilter);
                Blur(ref bloomA, ref bloomB, bloomRadiusPixels, downsample,
                    bloomIterations, true, true);
                material.SetTexture(BloomTexId, bloomA);

                int wideDownsample = Mathf.Min(downsample, 8) * 4;
                int wideWidth = Mathf.Max(1, source.width / wideDownsample);
                int wideHeight = Mathf.Max(1, source.height / wideDownsample);
                wideA = GetTemporary(source, wideWidth, wideHeight, FilterMode.Bilinear);
                wideB = GetTemporary(source, wideWidth, wideHeight, FilterMode.Bilinear);
                material.SetVector(SourceToTargetScaleId,
                    new Vector4(source.width / (float)wideWidth,
                        source.height / (float)wideHeight, 0f, 0f));
                Graphics.Blit(source, wideA, material, PassBloomPrefilter);
                Blur(ref wideA, ref wideB, wideBloomRadiusPixels, wideDownsample,
                    bloomIterations, true, true);
                material.SetTexture(BloomWideTexId, wideA);
            }
            else
            {
                material.SetTexture(BloomTexId, Texture2D.blackTexture);
                material.SetTexture(BloomWideTexId, Texture2D.blackTexture);
            }

            if (AutoExposureActive)
            {
                UpdateExposure(source, deltaTime);
                for (int index = 0; index < MaximumLensDefinitions; index++)
                    material.SetTexture(ExposureTexIds[index], exposureRead[index]);
            }
            else
            {
                exposureHistoryValid = false;
                for (int index = 0; index < MaximumLensDefinitions; index++)
                    material.SetTexture(ExposureTexIds[index], Texture2D.blackTexture);
            }

            Graphics.Blit(source, destination, material, PassComposite);
        }
        finally
        {
            ReleaseTemporary(nearA);
            ReleaseTemporary(nearB);
            ReleaseTemporary(hazeA);
            ReleaseTemporary(hazeB);
            ReleaseTemporary(bloomA);
            ReleaseTemporary(bloomB);
            ReleaseTemporary(wideA);
            ReleaseTemporary(wideB);
        }
    }

    private void SetFrameProperties(RenderTexture source)
    {
        material.SetVector(SourceSizeId,
            new Vector4(source.width, source.height, 1f / source.width, 1f / source.height));
        material.SetVector("_LensCenter",
            new Vector4(opticTextureCenter.x, opticTextureCenter.y, 0f, 0f));

        float opticAspect = lensTexture.height > 0
            ? lensTexture.width / (float)lensTexture.height
            : 1f;
        material.SetTexture("_LensTexture", lensTexture);
        material.SetTexture("_MaskOverlay", maskOverlay);
        material.SetFloat("_OpticTextureAspect", opticAspect);
        material.SetFloat("_OpticTextureScale", opticTextureScale);
        material.SetFloat("_LensAlphaCutoff", lensAlphaCutoff);
        material.SetFloat("_LensAlphaFeather", lensAlphaFeather);
        SetLensLayoutProperties();

        material.SetFloat("_NearBlurEnabled", NearPassActive ? 1f : 0f);
        material.SetFloat("_NearBlurStart", fullBlurDistance);
        material.SetFloat("_NearBlurEnd", sharpDistance);
        material.SetFloat("_NearBlurStrength", nearBlurStrength);
        material.SetFloat("_ForegroundExpansion", foregroundExpansion);

        material.SetFloat("_OpticalHazeEnabled", opticalHazeEnabled ? 1f : 0f);
        material.SetFloat("_OpticalHazeCenterStrength", opticalHazeCenterStrength);
        material.SetFloat("_OpticalHazeEdgeStrength", opticalHazeEdgeStrength);
        material.SetFloat("_OpticalHazeEdgeWidthPixels", opticalHazeEdgeWidthPixels);
        material.SetFloat("_OpticalHazeFalloff", opticalHazeFalloff);
        material.SetFloat("_OpticalHazeVeilStrength", opticalHazeVeilStrength);
        material.SetFloat("_ChromaticAberrationPixels",
            opticalHazeChromaticAberrationPixels);

        material.SetFloat("_BloomEnabled", BloomPassActive ? 1f : 0f);
        material.SetFloat("_BloomThreshold", bloomThreshold);
        material.SetFloat("_BloomSoftKnee", bloomSoftKnee);
        material.SetFloat("_BloomIntensity", bloomIntensity);

        material.SetVector("_SpectralSensitivity",
            new Vector4(spectralSensitivity.x, spectralSensitivity.y, spectralSensitivity.z, 0f));
        material.SetColor("_PhosphorTint", phosphorTint);
        material.SetFloat("_ManualGain", manualGain);

        material.SetFloat("_AutoExposure", AutoExposureActive ? 1f : 0f);
        material.SetFloat("_ExposureTarget", targetLuminance);
        material.SetVector("_ExposureEVMinMax",
            new Vector4(minimumExposureEV, maximumExposureEV, 0f, 0f));
        material.SetVector("_AdaptationSpeeds", new Vector4(brightSceneSpeed, darkSceneSpeed, 0f, 0f));
        material.SetFloat("_HighlightProtection", highlightProtection);

        material.SetFloat("_ReadNoise", readNoise);
        material.SetFloat("_ShotNoise", shotNoise);
        material.SetFloat("_CoarseNoise", coarseNoise);
        material.SetFloat("_FixedPatternNoise", fixedPatternNoise);
        material.SetFloat("_Scintillation", scintillation);
        material.SetFloat("_ScintillationDensity", scintillationDensity);
        material.SetFloat("_NoiseFadeStartLuminance", noiseFadeStartLuminance);
        material.SetFloat("_NoiseFadeEndLuminance", noiseFadeEndLuminance);
        material.SetFloat("_GrainPixelSize", grainPixelSize);
        material.SetFloat("_NoiseRefreshRate", noiseRefreshRate);
        material.SetFloat("_EffectTime", CurrentTime());
    }

    private void SetLensLayoutProperties()
    {
        EnsureLensDefinitionArray();
        Vector4 enabledSlots = Vector4.zero;
        for (int index = 0; index < MaximumLensDefinitions; index++)
        {
            LensDefinition definition = lensDefinitions[index];
            if (!definition.enabled || definition.radiusInTextureHeights <= 0.0001f)
            {
                lensDefinitionUpload[index] = Vector4.zero;
                continue;
            }

            lensDefinitionUpload[index] = new Vector4(
                definition.centerUV.x,
                definition.centerUV.y,
                definition.radiusInTextureHeights,
                definition.distortionMultiplier);
            enabledSlots[index] = 1f;
        }

        material.SetVector("_LensDefinition0", lensDefinitionUpload[0]);
        material.SetVector("_LensDefinition1", lensDefinitionUpload[1]);
        material.SetVector("_LensDefinition2", lensDefinitionUpload[2]);
        material.SetVector("_LensDefinition3", lensDefinitionUpload[3]);
        material.SetVector("_LensDefinitionEnabled", enabledSlots);
        material.SetVector("_LensFusionGroups", new Vector4(
            lensDefinitions[0].fusionGroup,
            lensDefinitions[1].fusionGroup,
            lensDefinitions[2].fusionGroup,
            lensDefinitions[3].fusionGroup));
        material.SetVector("_LensVignetteMultipliers", new Vector4(
            lensDefinitions[0].vignetteMultiplier,
            lensDefinitions[1].vignetteMultiplier,
            lensDefinitions[2].vignetteMultiplier,
            lensDefinitions[3].vignetteMultiplier));
        material.SetFloat("_LensEdgeDistortionEnabled", multiLensEdgeDistortion ? 1f : 0f);
        material.SetFloat("_MultiLensDistortionStrengthPixels", multiLensDistortionStrengthPixels);
        material.SetFloat("_MultiLensDistortionWidthPixels", multiLensDistortionWidthPixels);
        material.SetFloat("_MultiLensDistortionFalloff", multiLensDistortionFalloff);
        material.SetFloat("_LensSeamMode", (float)lensSeamMode);
        material.SetFloat("_LensSeamWidth", lensSeamWidth);
        material.SetFloat("_LensSeamOpacity", lensSeamOpacity);
        material.SetColor("_LensSeamColor", lensSeamColor);
        material.SetFloat("_MultiLensVignetteEnabled", multiLensVignette ? 1f : 0f);
        material.SetFloat("_MultiLensOuterVignetteWidth", multiLensOuterVignetteWidth);
        material.SetFloat("_MultiLensOuterVignetteStrength", multiLensOuterVignetteStrength);
        material.SetFloat("_MultiLensVignetteFalloff", multiLensVignetteFalloff);
    }

    private void EnsureLensDefinitionArray()
    {
        if (lensDefinitions != null && lensDefinitions.Length == MaximumLensDefinitions)
            return;

        LensDefinition[] resized = new LensDefinition[MaximumLensDefinitions];
        if (lensDefinitions != null)
        {
            int copyCount = Mathf.Min(lensDefinitions.Length, MaximumLensDefinitions);
            for (int index = 0; index < copyCount; index++)
                resized[index] = lensDefinitions[index];
        }
        lensDefinitions = resized;
    }

    private static bool IsValidPreset(LensLayoutPreset preset)
    {
        return preset == LensLayoutPreset.Pvs14 ||
               preset == LensLayoutPreset.Gpnvg ||
               preset == LensLayoutPreset.DualTube ||
               preset == LensLayoutPreset.Custom;
    }

    private static bool IsValidSeamMode(LensSeamMode mode)
    {
        return mode == LensSeamMode.None || mode == LensSeamMode.Soft ||
               mode == LensSeamMode.Dark || mode == LensSeamMode.Hard;
    }

    private static bool TryGetPresetLensDefinition(LensLayoutPreset preset, int index,
        out LensDefinition definition)
    {
        definition = default(LensDefinition);
        if (preset == LensLayoutPreset.Pvs14 && index == 0)
        {
            definition = new LensDefinition(
                new Vector2(0.5f, 0.5f), 0.372265625f, 1f, 0);
            return true;
        }

        if (preset == LensLayoutPreset.DualTube)
        {
            if (index == 0)
                definition = new LensDefinition(
                    new Vector2(0.475f, 0.5f), 0.372265625f, 1f, 1);
            else if (index == 1)
                definition = new LensDefinition(
                    new Vector2(0.525f, 0.5f), 0.372265625f, 1f, 1);
            else
                return false;
            return true;
        }

        if (preset != LensLayoutPreset.Gpnvg)
            return false;

        if (index == 0)
            definition = new LensDefinition(
                new Vector2(0.204605263f, 0.5f), 0.3875f, 1f, 0);
        else if (index == 1)
            definition = new LensDefinition(
                new Vector2(0.475f, 0.5f), 0.3875f, 1f, 1);
        else if (index == 2)
            definition = new LensDefinition(
                new Vector2(0.525f, 0.5f), 0.3875f, 1f, 1);
        else if (index == 3)
            definition = new LensDefinition(
                new Vector2(0.795394737f, 0.5f), 0.3875f, 1f, 2);
        else
            return false;
        return true;
    }

    private void ApplyBuiltInPreset(LensLayoutPreset preset)
    {
        EnsureLensDefinitionArray();
        if (!IsValidPreset(preset))
        {
            lensLayoutPreset = LensLayoutPreset.Pvs14;
            preset = LensLayoutPreset.Pvs14;
        }
        if (preset == LensLayoutPreset.Custom)
            return;

        for (int index = 0; index < MaximumLensDefinitions; index++)
            lensDefinitions[index] = default(LensDefinition);

        for (int index = 0; index < MaximumLensDefinitions; index++)
        {
            LensDefinition definition;
            if (TryGetPresetLensDefinition(preset, index, out definition))
                lensDefinitions[index] = definition;
        }
    }

    private void BlurFromFullResolutionSource(RenderTexture fullResolutionSource,
        RenderTexture horizontalTarget, RenderTexture verticalTarget,
        float radiusPixels, int downsample, int iterations,
        bool isolateLensDomains, bool packFusionChannels)
    {
        int safeDownsample = Mathf.Max(1, downsample);
        int safeIterations = Mathf.Max(1, iterations);
        float fullResolutionScale = radiusPixels /
                                    (GaussianKernelExtent * Mathf.Sqrt(safeIterations));
        float downsampledScale = fullResolutionScale / safeDownsample;
        SetBlurMode(isolateLensDomains, packFusionChannels);

        for (int iteration = 0; iteration < safeIterations; iteration++)
        {
            RenderTexture horizontalSource = iteration == 0
                ? fullResolutionSource
                : verticalTarget;
            float horizontalScale = iteration == 0
                ? fullResolutionScale
                : downsampledScale;
            material.SetVector(BlurDirectionId,
                new Vector4(horizontalScale, 0f, 0f, 0f));
            Graphics.Blit(horizontalSource, horizontalTarget, material, PassGaussian);

            material.SetVector(BlurDirectionId,
                new Vector4(0f, downsampledScale, 0f, 0f));
            Graphics.Blit(horizontalTarget, verticalTarget, material, PassGaussian);
        }
    }

    private void Blur(ref RenderTexture source, ref RenderTexture destination,
        float radiusPixels, int downsample, int iterations,
        bool isolateLensDomains, bool packFusionChannels)
    {
        int safeDownsample = Mathf.Max(1, downsample);
        int safeIterations = Mathf.Max(1, iterations);
        float directionScale = radiusPixels /
                               (GaussianKernelExtent * safeDownsample * Mathf.Sqrt(safeIterations));
        SetBlurMode(isolateLensDomains, packFusionChannels);

        for (int iteration = 0; iteration < safeIterations; iteration++)
        {
            material.SetVector(BlurDirectionId, new Vector4(directionScale, 0f, 0f, 0f));
            Graphics.Blit(source, destination, material, PassGaussian);
            Swap(ref source, ref destination);

            material.SetVector(BlurDirectionId, new Vector4(0f, directionScale, 0f, 0f));
            Graphics.Blit(source, destination, material, PassGaussian);
            Swap(ref source, ref destination);
        }
    }

    private void SetBlurMode(bool isolateLensDomains, bool packFusionChannels)
    {
        material.SetFloat(BlurDomainIsolationId, isolateLensDomains ? 1f : 0f);
        material.SetFloat(BlurPackFusionChannelsId, packFusionChannels ? 1f : 0f);
    }

    private void UpdateExposure(RenderTexture source, float deltaTime)
    {
        EnsureExposureBuffers();
        int luminanceSize = LargestPowerOfTwoAtMost(Mathf.Min(64, Mathf.Min(source.width, source.height)));
        for (int lensIndex = 0; lensIndex < MaximumLensDefinitions; lensIndex++)
        {
            LensDefinition definition = lensDefinitions[lensIndex];
            if (!definition.enabled || definition.radiusInTextureHeights <= 0.0001f)
                continue;

            RenderTexture luminance = null;
            try
            {
                material.SetFloat(ExposureLensIndexId, lensIndex);
                luminance = GetTemporary(source, luminanceSize, luminanceSize, FilterMode.Bilinear);
                material.SetVector(SourceToTargetScaleId,
                    new Vector4(source.width / (float)luminanceSize,
                        source.height / (float)luminanceSize, 0f, 0f));
                Graphics.Blit(source, luminance, material, PassLuminancePrefilter);

                int size = luminanceSize;
                while (size > 1)
                {
                    int nextSize = Mathf.Max(1, size / 2);
                    RenderTexture next = GetTemporary(source, nextSize, nextSize, FilterMode.Bilinear);
                    Graphics.Blit(luminance, next, material, PassLuminanceDownsample);
                    ReleaseTemporary(luminance);
                    luminance = next;
                    size = nextSize;
                }

                material.SetTexture(ExposureHistoryId, exposureRead[lensIndex]);
                material.SetFloat("_ExposureHistoryValid", exposureHistoryValid ? 1f : 0f);
                material.SetFloat("_DeltaTime", deltaTime);
                Graphics.Blit(luminance, exposureWrite[lensIndex], material, PassExposureAdapt);
                Swap(ref exposureRead[lensIndex], ref exposureWrite[lensIndex]);
            }
            finally
            {
                ReleaseTemporary(luminance);
            }
        }
        exposureHistoryValid = true;
    }

    private void EnsureExposureBuffers()
    {
        bool buffersReady = true;
        for (int index = 0; index < MaximumLensDefinitions; index++)
        {
            buffersReady &= exposureRead[index] != null && exposureWrite[index] != null &&
                exposureRead[index].IsCreated() && exposureWrite[index].IsCreated();
        }
        if (buffersReady)
            return;

        ReleaseExposureBuffers();
        for (int index = 0; index < MaximumLensDefinitions; index++)
        {
            exposureRead[index] = CreateExposureTexture(
                "Night Vision Exposure " + index + " A");
            exposureWrite[index] = CreateExposureTexture(
                "Night Vision Exposure " + index + " B");
            ClearTexture(exposureRead[index]);
            ClearTexture(exposureWrite[index]);
        }
        exposureHistoryValid = false;
    }

    private RenderTexture CreateExposureTexture(string textureName)
    {
        RenderTexture texture = new RenderTexture(1, 1, 0, intermediateFormat, RenderTextureReadWrite.Linear);
        texture.name = textureName;
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.useMipMap = false;
        texture.autoGenerateMips = false;
        texture.Create();
        return texture;
    }

    private static void ClearTexture(RenderTexture texture)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = texture;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = previous;
    }

    private void ReleaseExposureBuffers()
    {
        for (int index = 0; index < MaximumLensDefinitions; index++)
        {
            ReleasePersistent(ref exposureRead[index]);
            ReleasePersistent(ref exposureWrite[index]);
        }
        exposureHistoryValid = false;
    }

    private static void ReleasePersistent(ref RenderTexture texture)
    {
        if (texture == null)
            return;
        texture.Release();
        if (Application.isPlaying)
            Destroy(texture);
        else
            DestroyImmediate(texture);
        texture = null;
    }

    private RenderTexture GetTemporary(RenderTexture source, int width, int height, FilterMode filterMode)
    {
        RenderTextureDescriptor descriptor = source.descriptor;
        descriptor.width = Mathf.Max(1, width);
        descriptor.height = Mathf.Max(1, height);
        descriptor.depthBufferBits = 0;
        descriptor.msaaSamples = 1;
        descriptor.useMipMap = false;
        descriptor.autoGenerateMips = false;
        descriptor.enableRandomWrite = false;
        descriptor.colorFormat = intermediateFormat;

        RenderTexture texture = RenderTexture.GetTemporary(descriptor);
        texture.filterMode = filterMode;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    private static void ReleaseTemporary(RenderTexture texture)
    {
        if (texture != null)
            RenderTexture.ReleaseTemporary(texture);
    }

    private float CalculateDeltaTime()
    {
        float now = CurrentTime();
        float deltaTime = previousRenderTime < 0f ? 1f / 60f : now - previousRenderTime;
        previousRenderTime = now;

        if (deltaTime <= 0f || deltaTime > 0.5f)
        {
            exposureHistoryValid = false;
            return 1f / 60f;
        }

        return Mathf.Clamp(deltaTime, 1f / 240f, 0.1f);
    }

    private static float CurrentTime()
    {
        return Application.isPlaying ? Time.unscaledTime : Time.realtimeSinceStartup;
    }

    private static int LargestPowerOfTwoAtMost(int value)
    {
        value = Mathf.Max(1, value);
        int result = 1;
        while (result <= value / 2)
            result *= 2;
        return result;
    }

    private static void Swap(ref RenderTexture a, ref RenderTexture b)
    {
        RenderTexture temporary = a;
        a = b;
        b = temporary;
    }
}
}
