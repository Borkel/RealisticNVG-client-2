using BorkelRNVG.Helpers;
using BorkelRNVG.Configuration;
using BorkelRNVG.Enum;
using BorkelRNVG.Globals;
using BorkelRNVG.Models;
using BSG.CameraEffects;
using EFT;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace BorkelRNVG.Controllers
{
    public class AutoGatingController : MonoBehaviour
    {
        public static AutoGatingController Instance;

        public float GatingMultiplier = 1.0f;

        // component vars
        public Camera mainCamera;
        public NightVision nightVision;

        // computeshader vars
        public ComputeShader computeShader = FileHelper.LoadComputeShader("assets/shaders/pein/shaders/brightnessshader.compute", Path.Combine(ModDirectories.ShadersPath, "pein_shaders"));
        private ComputeBuffer _brightnessBuffer;
        private CommandBuffer _commandBuffer;
        private const float BRIGHTNESS_SCALE = 10000f;
        private int _kernel;

        // various other vars
        private float _currentBrightness = 1.0f; // value that GatingMultiplier gets lerped to
        public float gateSpeed = 0.3f; // lerp speed
        public float maxBrightnessMult = 1f; // max gatingmult
        public float minBrightnessMult = 0.2f; // min gatingmult
        public float minInput = 0f; // min _currentBrightness
        public float maxInput = 0.1f; // max _currentBrightness
        private int _frameInterval = 5; // interval between shader dispatches
        private int _frameCount = 0;

        private int _textureWidth = Screen.width / 8;
        private int _textureHeight = Screen.height / 8;

        public Material blurMaterial;
        public Material additiveBlendMaterial;
        public Material contrastMaterial;
        public Material exposureMaterial;
        public Material maskMaterial;

        public RenderTexture renderTexture; // FINAL TEXTURE
        public RenderTexture blurTexture1;
        public RenderTexture blurTexture2;
        public RenderTexture contrastTexture;
        public RenderTexture exposureTexture;
        public RenderTexture maskTexture;

        public float blurSize = 8f;
        public float contrastLevel = 3f;
        public float exposureAmount = 4f;

        public static AutoGatingController Create()
        {
            GameObject camera = CameraClass.Instance.Camera.gameObject;
            AutoGatingController autoGatingController = camera.AddComponent<AutoGatingController>();
            return autoGatingController;
        }

        public IEnumerator AdjustAutoGating(float delay, float multiplier, NvgData nvgData)
        {
            yield return new WaitForSeconds(delay);

            if (Plugin.clampMinGating.Value)
            {
                float newBrightness = Mathf.Clamp(GatingMultiplier * multiplier, nvgData.NightVisionConfig.MinBrightness.Value, nvgData.NightVisionConfig.MaxBrightness.Value);
                GatingMultiplier = newBrightness;
            }
            else
            {
                float newBrightness = Mathf.Clamp(GatingMultiplier * multiplier, 0f, nvgData.NightVisionConfig.MaxBrightness.Value);
                GatingMultiplier = newBrightness;
            }
        }
        
        public void SetEnabled(bool state)
        {
            enabled = state;
            
            if (enabled) return;
            
            _currentBrightness = 1f;
            GatingMultiplier = 1f;
        }

        public void ApplySettings(NightVisionConfig config)
        {
            EGatingType gatingType = config.AutoGatingType.Value;
            if (gatingType != EGatingType.Off)
            {
                gateSpeed = config.GatingSpeed.Value;
                maxBrightnessMult = config.MaxBrightness.Value;
                minBrightnessMult = config.MinBrightness.Value;
                minInput = config.MinBrightnessThreshold.Value;
                maxInput = config.MaxBrightnessThreshold.Value;
                enabled = true;
            }
            else
            {
                _currentBrightness = 1.0f;
                GatingMultiplier = 1.0f;
                enabled = false;
            }
        }

        public void SetBrightnessTarget(float target)
        {
            _currentBrightness = target;
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            mainCamera = CameraClass.Instance.Camera;
            nightVision = CameraClass.Instance.NightVision;

            // everything beyond this point makes my head hurt
            renderTexture = CreateRenderTexture();

            _commandBuffer = new CommandBuffer { name = "AutoGatingCommandBuffer" };

            blurTexture1 = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            blurTexture2 = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            contrastTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            exposureTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            maskTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);

            contrastMaterial = new Material(AssetHelper.contrastShader) { name = "ContrastMaterial" };
            blurMaterial = new Material(AssetHelper.blurShader) { name = "BlurMaterial" };
            additiveBlendMaterial = new Material(AssetHelper.additiveBlendShader) { name = "AdditiveBlendMaterial" };
            exposureMaterial = new Material(AssetHelper.exposureShader) { name = "ExposureMaterial" };
            maskMaterial = new Material(AssetHelper.maskShader) { name = "MaskShader" };

            SetupCommandBuffer();

            _brightnessBuffer = new ComputeBuffer(1, sizeof(uint));

            _kernel = computeShader.FindKernel("CSReduceBrightness");
            computeShader.SetInt("_Width", _textureWidth);
            computeShader.SetInt("_Height", _textureHeight);

            // rendertexture debug
            if (Plugin.gatingDebug.Value == true)
            {
                var canvas = new GameObject("Canvas", typeof(Canvas)).GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var rawImage = new GameObject("RawImage", typeof(RawImage)).GetComponent<RawImage>();
                rawImage.transform.SetParent(canvas.transform);

                rawImage.rectTransform.sizeDelta = new Vector2(500, 500);
                rawImage.rectTransform.anchoredPosition = new Vector2(700, 0);

                rawImage.texture = renderTexture;
            }
        }

        private void SetupCommandBuffer()
        {
            _commandBuffer.Clear();

            _commandBuffer.Blit(BuiltinRenderTextureType.CurrentActive, exposureTexture, exposureMaterial);

            _commandBuffer.Blit(exposureTexture, contrastTexture, contrastMaterial);

            _commandBuffer.Blit(contrastTexture, blurTexture1, blurMaterial); // blur pass 1
            _commandBuffer.Blit(blurTexture1, blurTexture2, blurMaterial); // blur pass 2

            additiveBlendMaterial.SetTexture("_MainTex", contrastTexture);
            additiveBlendMaterial.SetTexture("_AddTex", blurTexture2);

            _commandBuffer.Blit(contrastTexture, renderTexture, additiveBlendMaterial);

            maskMaterial.SetTexture("_BaseTex", renderTexture);
            maskMaterial.SetTexture("_MaskTex", maskTexture);

            _commandBuffer.Blit(renderTexture, renderTexture, maskMaterial);

            mainCamera.AddCommandBuffer(CameraEvent.BeforeImageEffects, _commandBuffer);
        }

        public IEnumerator ComputeBrightness()
        {
            uint[] bufferData = new uint[1];
            _brightnessBuffer.SetData(bufferData);

            int threadGroupsX = Mathf.CeilToInt(_textureWidth / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(_textureHeight / 8.0f);
            computeShader.SetTexture(_kernel, "_InputTexture", renderTexture);
            computeShader.SetBuffer(_kernel, "_BrightnessBuffer", _brightnessBuffer);
            computeShader.Dispatch(_kernel, threadGroupsX, threadGroupsY, 1);

            // wait a frame (is this even necessary?)
            yield return new WaitForEndOfFrame();

            _brightnessBuffer.GetData(bufferData);

            uint totalBrightness = bufferData[0];
            float avgBrightness = totalBrightness / (_textureWidth * _textureHeight * BRIGHTNESS_SCALE);

            _currentBrightness = avgBrightness;
        }

        private void FixedUpdate()
        {
            if (!Plugin.nvgOn || !Plugin.enableAutoGating.Value)
            {
                _currentBrightness = 1f;
                GatingMultiplier = 1f;
            }
            
            string nvgId = PlayerHelper.GetCurrentNvgItemId();
            if (nvgId == null) return;

            NvgData nvgData = NvgHelper.GetNvgData(nvgId);
            if (nvgData == null) return;

            EGatingType gatingType = nvgData.NightVisionConfig.AutoGatingType.Value;
            bool nvgGatingEnabled = gatingType == EGatingType.AutoGating || gatingType == EGatingType.AutoGain;

            if (!nvgGatingEnabled)
            {
                _currentBrightness = 1f;
                GatingMultiplier = 1f;
                return;
            }

            _frameCount++;
            if (_frameCount >= _frameInterval)
            {
                _frameCount = 0;
                StartCoroutine(ComputeBrightness());
            }

            contrastMaterial.SetFloat("_Amount", contrastLevel);
            blurMaterial.SetFloat("_BlurSize", blurSize);
            exposureMaterial.SetFloat("_Exposure", exposureAmount);
            maskMaterial.SetTexture("_OverlayTex", nightVision.Material_0.GetTexture(Shader.PropertyToID("_Mask")));

            float gatingTarget = Mathf.Lerp(maxBrightnessMult, minBrightnessMult, Mathf.Clamp((_currentBrightness - minInput) / (maxInput - minInput), 0.0f, 1.0f));

            GatingMultiplier = Mathf.Lerp(GatingMultiplier, gatingTarget, gateSpeed);

            nightVision.ApplySettings();
        }

        private RenderTexture CreateRenderTexture()
        {
            RenderTexture rt = new RenderTexture(_textureWidth, _textureHeight, 24, RenderTextureFormat.ARGB32);
            rt.enableRandomWrite = true;
            rt.useMipMap = false;
            rt.autoGenerateMips = false;
            rt.filterMode = FilterMode.Point;
            rt.Create();

            return rt;
        }

        private void OnDestroy()
        {
            // get rid of this shit...
            renderTexture?.Release();
            blurTexture1?.Release();
            blurTexture2?.Release();
            contrastTexture?.Release();
            exposureTexture?.Release();
            maskTexture?.Release();
            _brightnessBuffer?.Release();
            mainCamera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, _commandBuffer);
            _commandBuffer?.Release();
        }
    }
}
