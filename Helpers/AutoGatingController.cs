﻿using BorkelRNVG.Helpers;
using BorkelRNVG.Helpers.Configuration;
using BSG.CameraEffects;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace BorkelRNVG
{
    public class AutoGatingController : MonoBehaviour
    {
        public static AutoGatingController Instance;

        public float GatingMultiplier = 1.0f;

        // component vars
        public Camera mainCamera;
        public NightVision nightVision;

        // computeshader vars
        public ComputeShader computeShader = AssetHelper.LoadComputeShader("assets/shaders/pein/shaders/brightnessshader.compute", $"{AssetHelper.assetsDirectory}\\Shaders\\pein_shaders");
        public ComputeBuffer brightnessBuffer;
        public CommandBuffer commandBuffer;
        private const float BRIGHTNESS_SCALE = 10000f;
        private int kernel;

        // various other vars
        private float _currentBrightness = 1.0f; // value that GatingMultiplier gets lerped to
        public float gateSpeed = 0.3f; // lerp speed
        public float maxBrightnessMult = 1f; // max gatingmult
        public float minBrightnessMult = 0.2f; // min gatingmult
        public float minInput = 0f; // min _currentBrightness
        public float maxInput = 0.1f; // max _currentBrightness
        private int _frameInterval = 5; // interval between shader dispatches
        private int _frameCount = 0;

        private int textureWidth = Screen.width / 8;
        private int textureHeight = Screen.height / 8;

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
        ComputeBuffer outputBuffer = new ComputeBuffer(1, sizeof(uint)); // float4 (RGBA) = 4 * sizeof(float)

        public static AutoGatingController Create()
        {
            GameObject camera = CameraClass.Instance.Camera.gameObject;
            AutoGatingController autoGatingController = camera.AddComponent<AutoGatingController>();
            return autoGatingController;
        }

        public void ApplySettings(NightVisionConfig config)
        {
            gateSpeed = config.GatingSpeed.Value;
            maxBrightnessMult = config.MaxBrightness.Value;
            minBrightnessMult = config.MinBrightness.Value;
            minInput = config.MinBrightnessThreshold.Value;
            maxInput = config.MaxBrightnessThreshold.Value;
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

            commandBuffer = new CommandBuffer {name = "AutoGatingCommandBuffer"};

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

            brightnessBuffer = new ComputeBuffer(1, sizeof(uint));

            kernel = computeShader.FindKernel("CSReduceBrightness");
            computeShader.SetInt("_Width", textureWidth);
            computeShader.SetInt("_Height", textureHeight);

            // rendertexture debug
            /*
            var canvas = new GameObject("Canvas", typeof(Canvas)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var rawImage = new GameObject("RawImage", typeof(RawImage)).GetComponent<RawImage>();
            rawImage.transform.SetParent(canvas.transform);

            rawImage.rectTransform.sizeDelta = new Vector2(500, 500);
            rawImage.rectTransform.anchoredPosition = new Vector2(700, 0);

            rawImage.texture = renderTexture;*/
        }

        private void SetupCommandBuffer()
        {
            commandBuffer.Clear();
            
            commandBuffer.Blit(BuiltinRenderTextureType.CurrentActive, exposureTexture, exposureMaterial);

            commandBuffer.Blit(exposureTexture, contrastTexture, contrastMaterial);

            commandBuffer.Blit(contrastTexture, blurTexture1, blurMaterial); // blur pass 1
            commandBuffer.Blit(blurTexture1, blurTexture2, blurMaterial); // blur pass 2

            additiveBlendMaterial.SetTexture("_MainTex", contrastTexture);
            additiveBlendMaterial.SetTexture("_AddTex", blurTexture2);

            commandBuffer.Blit(contrastTexture, renderTexture, additiveBlendMaterial);

            maskMaterial.SetTexture("_BaseTex", renderTexture);
            maskMaterial.SetTexture("_MaskTex", maskTexture);

            commandBuffer.Blit(renderTexture, renderTexture, maskMaterial);

            mainCamera.AddCommandBuffer(CameraEvent.BeforeImageEffects, commandBuffer);
        }

        public IEnumerator ComputeBrightness()
        {
            uint[] bufferData = new uint[1];
            brightnessBuffer.SetData(bufferData);

            int threadGroupsX = Mathf.CeilToInt(textureWidth / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(textureHeight / 8.0f);
            computeShader.SetTexture(kernel, "_InputTexture", renderTexture);
            computeShader.SetBuffer(kernel, "_BrightnessBuffer", brightnessBuffer);
            computeShader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);

            // wait a frame (is this even necessary?)
            yield return new WaitForEndOfFrame();

            brightnessBuffer.GetData(bufferData);

            uint totalBrightness = bufferData[0];
            float avgBrightness = (float)totalBrightness / (textureWidth * textureHeight * BRIGHTNESS_SCALE);

            _currentBrightness = avgBrightness;
        }

        private void FixedUpdate()
        {
            if (!Plugin.nvgOn) return;

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
            RenderTexture rt = new RenderTexture(textureWidth, textureHeight, 24, RenderTextureFormat.ARGB32);
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
            brightnessBuffer?.Release();
            mainCamera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, commandBuffer);
            commandBuffer?.Release();
        }
    }
}