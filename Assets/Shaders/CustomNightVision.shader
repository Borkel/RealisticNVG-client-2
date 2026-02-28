// Written by Arys
Shader "Hidden/CustomNightVision"
{
	Properties
	{
		// Nightvision properties
		_Color ("Color", Vector) = (0.596,0.839,0.988,1)
		_MainTex ("_MainTex", 2D) = "white" {}
		_Intensity ("_Intensity", Float) = 2.5
		_Noise ("_Noise", 2D) = "white" {}
		_NoiseScale ("_NoiseScale", Vector) = (1,1.68,0,0)
		_NoiseIntensity ("_NoiseIntensity", Float) = 1
		_NightVisionOn ("_NightVisionOn", Float) = 1
		_EdgeDistortion ("_EdgeDistortion", Float) = 0.1
		_EdgeDistortionStart ("_EdgeDistortionStart", Float) = 0.28
		_NearBlurIntensity ("_NearBlurIntensity", Float) = 20
		_NearBlurMaxDistance ("_NearBlurMaxDistance", Float) = 4
		// Texture mask properties
		_Mask ("_Mask", 2D) = "white" {}
		_InvMaskSize ("_InvMaskSize", Float) = 1
		_InvAspect ("_InvAspect", Float) = 0.42
		_CameraAspect ("_CameraAspect", Float) = 1.78
	}
	SubShader
	{
		Pass
		{
			Cull Off
			ZWrite Off
			ZTest Always
			Fog
			{
				Mode Off
			}
			GpuProgramID 33735
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct v2f
			{
				float4 sv_position : SV_Position0;
				float2 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
				float2 texcoord2 : TEXCOORD2;
				float4 texcoord3 : TEXCOORD3;
			};

			struct fout
			{
				float4 sv_target : SV_Target0;
			};

			float2 _NoiseScale;
			float _NoiseIntensity;
			float _NightVisionOn;
			float _EdgeDistortion;
			float _EdgeDistortionStart;
			float _NearBlurIntensity;
			float _NearBlurMaxDistance;
			float4 _Color;
			float _Intensity;
			float4 _MainTex_TexelSize;
			float _InvMaskSize;
			float _InvAspect;
			float _CameraAspect;
			sampler2D _MainTex;
			sampler2D _Mask;
			sampler2D _Noise;
			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

			float ComputeNearFactor(float2 uv)
			{
				float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
				float eyeDepth = LinearEyeDepth(rawDepth);
				return saturate(1.0 - eyeDepth / max(_NearBlurMaxDistance, 0.001));
			}

			v2f vert(appdata_full v)
			{
				v2f o;
				float4 tmp0;
				float4 tmp1;
				float4 tmp2;
				tmp0 = v.vertex.yyyy * unity_ObjectToWorld._m01_m11_m21_m31;
				tmp0 = unity_ObjectToWorld._m00_m10_m20_m30 * v.vertex.xxxx + tmp0;
				tmp0 = unity_ObjectToWorld._m02_m12_m22_m32 * v.vertex.zzzz + tmp0;
				tmp0 = tmp0 + unity_ObjectToWorld._m03_m13_m23_m33;
				tmp1 = tmp0.yyyy * unity_MatrixVP._m01_m11_m21_m31;
				tmp1 = unity_MatrixVP._m00_m10_m20_m30 * tmp0.xxxx + tmp1;
				tmp1 = unity_MatrixVP._m02_m12_m22_m32 * tmp0.zzzz + tmp1;
				tmp1 = unity_MatrixVP._m03_m13_m23_m33 * tmp0.wwww + tmp1;
				o.sv_position = tmp1;
				if (_NightVisionOn == 0)
				{
					o.texcoord.xy = v.texcoord.xy;
					return o;
				}
				tmp0.x = v.texcoord.x - 0.5;
				tmp0.x *= _CameraAspect;
				tmp0.z = tmp0.x * _InvAspect;
				tmp0.w = v.texcoord.y;
				tmp0.xy = tmp0.zw - float2(-0.0, 0.5);
				tmp2.xy = _Time.xx * float2(14345.68, -12345.68);
				tmp2.xy = frac(tmp2.xy);
				o.texcoord1.xy = v.texcoord.xy * _NoiseScale + tmp2.xy;
				o.texcoord2.xy = tmp0.xy * _InvMaskSize.xx + float2(0.5, 0.5);
				o.texcoord.xy = v.texcoord.xy;
				tmp0.y = tmp0.y * unity_MatrixV._m21;
				tmp0.x = unity_MatrixV._m20 * tmp0.x + tmp0.y;
				tmp0.x = unity_MatrixV._m22 * tmp0.z + tmp0.x;
				tmp0.x = unity_MatrixV._m23 * tmp0.w + tmp0.x;
				o.texcoord3.z = -tmp0.x;
				tmp0.x = tmp1.y * _ProjectionParams.x;
				tmp0.w = tmp0.x * 0.5;
				tmp0.xz = tmp1.xw * float2(0.5, 0.5);
				o.texcoord3.w = tmp1.w;
				o.texcoord3.xy = tmp0.zz + tmp0.xw;
				return o;
			}

			fout frag(v2f inp)
			{
				fout o;
				float4 tmp0 = tex2D(_MainTex, inp.texcoord.xy);
				if (_NightVisionOn == 0)
				{
					o.sv_target = tmp0;
					return o;
				}
				float4 noise = tex2D(_Noise, inp.texcoord1.xy);
				float4 rawMask = tex2D(_Mask, inp.texcoord2.xy);
				// 1) NVG application mask (hard cutoff).
				const float nvgCutoff = 0.55;
				float nvgMask = step(rawMask.a, nvgCutoff);
				if (nvgMask <= 0.0)
				{
					o.sv_target = tmp0;
					return o;
				}

				// Prebaked mask format:
				// R,G = edge direction encoded from [-1,1] to [0,1]
				// B   = distance-to-edge normalized [0,1] inside mask
				float2 edgeDir = rawMask.rg * 2.0 - 1.0;
				edgeDir.y = -edgeDir.y;
				float dirLen = length(edgeDir);
				if (dirLen > 1e-5)
				{
					edgeDir /= dirLen;
				}
				else
				{
					edgeDir = float2(0.0, 0.0);
				}

				float edgeDistance = saturate(rawMask.b);
				float edgeWidth = max(saturate(_EdgeDistortionStart), 0.001);
				float edgeBand = 1.0 - smoothstep(0.0, edgeWidth, edgeDistance);
				float edgeBias = 1.0 - edgeDistance;
				// 2) Distortion strength mask (smooth falloff).
				float distMask = nvgMask * edgeBand * edgeBias * edgeBias;
				float2 warpedUv = inp.texcoord.xy - edgeDir * (_EdgeDistortion * 0.1 * distMask);
				warpedUv = clamp(warpedUv, 0.0, 1.0);
				tmp0 = tex2D(_MainTex, warpedUv);

				// Depth-based near Gaussian blur (strong near camera, fades to zero at max distance).
				float nearFactor = ComputeNearFactor(warpedUv);
				float2 spreadUv = _MainTex_TexelSize.xy * max(1.0, _NearBlurIntensity * 0.35);
				float nC  = nearFactor;
				float nR  = ComputeNearFactor(clamp(warpedUv + float2( spreadUv.x, 0.0), 0.0, 1.0));
				float nL  = ComputeNearFactor(clamp(warpedUv + float2(-spreadUv.x, 0.0), 0.0, 1.0));
				float nU  = ComputeNearFactor(clamp(warpedUv + float2(0.0,  spreadUv.y), 0.0, 1.0));
				float nD  = ComputeNearFactor(clamp(warpedUv + float2(0.0, -spreadUv.y), 0.0, 1.0));
				float nUR = ComputeNearFactor(clamp(warpedUv + float2( spreadUv.x,  spreadUv.y), 0.0, 1.0));
				float nUL = ComputeNearFactor(clamp(warpedUv + float2(-spreadUv.x,  spreadUv.y), 0.0, 1.0));
				float nDR = ComputeNearFactor(clamp(warpedUv + float2( spreadUv.x, -spreadUv.y), 0.0, 1.0));
				float nDL = ComputeNearFactor(clamp(warpedUv + float2(-spreadUv.x, -spreadUv.y), 0.0, 1.0));
				float nearSoft = (
					nUL + 2.0 * nU + nUR +
					2.0 * nL + 4.0 * nC + 2.0 * nR +
					nDL + 2.0 * nD + nDR
				) / 16.0;
				float nearPeak = max(max(max(nL, nR), max(nU, nD)), max(max(nUL, nUR), max(nDL, nDR)));
				float nearFactorSpread = lerp(nearSoft, nearPeak, 0.35);
				float blurRadiusPx = _NearBlurIntensity * nearFactorSpread * nvgMask;
				if (blurRadiusPx > 0.001)
				{
					const int kernelRadius = 3; // 7x7 kernel
					float2 texel = _MainTex_TexelSize.xy;
					float sampleScale = blurRadiusPx / kernelRadius;
					float sigma = max(blurRadiusPx * 0.5, 0.75);
					float invTwoSigma2 = 0.5 / (sigma * sigma);
					float4 g = 0;
					float wsum = 0;
					[unroll]
					for (int ky = -kernelRadius; ky <= kernelRadius; ky++)
					{
						[unroll]
						for (int kx = -kernelRadius; kx <= kernelRadius; kx++)
						{
							float2 k = float2(kx, ky);
							float w = exp(-dot(k, k) * invTwoSigma2);
							float2 suv = clamp(warpedUv + k * texel * sampleScale, 0.0, 1.0);
							g += tex2D(_MainTex, suv) * w;
							wsum += w;
						}
					}
					float4 blurred = g / max(wsum, 1e-5);
					float blurBlend = smoothstep(0.0, 0.35, nearFactorSpread);
					tmp0 = lerp(tmp0, blurred, blurBlend);
				}

				noise *= _NoiseIntensity.xxxx;
				noise *= nvgMask;
				float4 tmp1 = tmp0;
				tmp1.x += tmp1.y;
				tmp1.x += tmp1.z;
				tmp1 = noise + tmp1.xxxx * _Color;
				tmp1 *= _Intensity.xxxx;
				tmp1 = saturate(tmp1 * 0.45);
				tmp0 = lerp(tmp0, tmp1, nvgMask);
				o.sv_target = tmp0;
				return o;
			}
			ENDCG
		}
	}
	Fallback "Hidden/Internal-BlackError"
}
