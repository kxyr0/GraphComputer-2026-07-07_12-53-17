Shader "GraphComputer/World/Panorama Background"
{
	Properties
	{
		[Header(Base Fill)]
		_BaseColor ("Base Color", Color) = (0.105, 0.108, 0.180, 1)
		_TopTint ("Top Tint", Color) = (0.120, 0.120, 0.250, 1)
		_BottomTint ("Bottom Tint", Color) = (0.045, 0.050, 0.110, 1)
		_GradientStrength ("Gradient Strength", Range(0, 1)) = 0.55

		[Header(Main Soft Glow)]
		_CenterGlowColor ("Center Glow Color", Color) = (0.360, 0.420, 1.000, 1)
		_CenterGlowStrength ("Center Glow Strength", Range(0, 1)) = 0.18
		_CenterGlowSize ("Center Glow Size", Range(0.1, 3)) = 1.15

		[Header(Side Accents)]
		_LeftGlowColor ("Left Glow Color", Color) = (0.000, 0.850, 1.000, 1)
		_LeftGlowStrength ("Left Glow Strength", Range(0, 1)) = 0.13
		_LeftGlowSize ("Left Glow Size", Range(0.1, 3)) = 1.35

		_RightGlowColor ("Right Glow Color", Color) = (0.750, 0.250, 1.000, 1)
		_RightGlowStrength ("Right Glow Strength", Range(0, 1)) = 0.11
		_RightGlowSize ("Right Glow Size", Range(0.1, 3)) = 1.45

		[Header(Subtle Band)]
		_BandColor ("Band Color", Color) = (0.150, 1.000, 0.950, 1)
		_BandStrength ("Band Strength", Range(0, 1)) = 0.055
		_BandWidth ("Band Width", Range(0.05, 1.5)) = 0.42
		_BandOffset ("Band Offset", Range(-1, 1)) = -0.08

		[Header(Soft Texture)]
		_NoiseStrength ("Noise Strength", Range(0, 0.2)) = 0.025
		_NoiseScale ("Noise Scale", Float) = 3.5
		_NoiseSpeed ("Noise Speed", Float) = 0.015

		[Header(Camera Parallax)]
		_CameraWorldPosition ("Camera World Position", Vector) = (0, 0, 0, 0)
		_Parallax ("Parallax", Float) = 0.006

		[Header(Final)]
		_VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.38
		_Brightness ("Brightness", Range(0, 2)) = 1.0
		_Alpha ("Alpha", Range(0, 1)) = 1.0
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent-100"
			"RenderType" = "Transparent"
			"IgnoreProjector" = "True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			Name "Simple Graph Background"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			struct v2f
			{
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			float4 _BaseColor;
			float4 _TopTint;
			float4 _BottomTint;
			float _GradientStrength;

			float4 _CenterGlowColor;
			float _CenterGlowStrength;
			float _CenterGlowSize;

			float4 _LeftGlowColor;
			float _LeftGlowStrength;
			float _LeftGlowSize;

			float4 _RightGlowColor;
			float _RightGlowStrength;
			float _RightGlowSize;

			float4 _BandColor;
			float _BandStrength;
			float _BandWidth;
			float _BandOffset;

			float _NoiseStrength;
			float _NoiseScale;
			float _NoiseSpeed;

			float4 _CameraWorldPosition;
			float _Parallax;

			float _VignetteStrength;
			float _Brightness;
			float _Alpha;

			v2f vert(appdata input)
			{
				v2f output;

				output.position = UnityObjectToClipPos(input.vertex);
				output.uv = input.uv;
				output.color = input.color;

				return output;
			}

			float Hash21(float2 p)
			{
				p = frac(p * float2(123.34, 456.21));
				p += dot(p, p + 45.32);
				return frac(p.x * p.y);
			}

			float Noise(float2 p)
			{
				float2 i = floor(p);
				float2 f = frac(p);

				float a = Hash21(i);
				float b = Hash21(i + float2(1.0, 0.0));
				float c = Hash21(i + float2(0.0, 1.0));
				float d = Hash21(i + float2(1.0, 1.0));

				float2 u = f * f * (3.0 - 2.0 * f);

				return lerp(
					lerp(a, b, u.x),
					lerp(c, d, u.x),
					u.y
				);
			}

			float SoftBlob(float2 uv, float2 center, float2 size)
			{
				float2 delta = (uv - center) / max(size, float2(0.001, 0.001));
				float distanceValue = dot(delta, delta);

				return saturate(1.0 - smoothstep(0.0, 1.0, distanceValue));
			}

			float4 frag(v2f input) : SV_Target
			{
				float2 uv = input.uv;

				float2 parallaxOffset = _CameraWorldPosition.xy * _Parallax;
				float2 animatedUv = uv + parallaxOffset;

				float3 color = _BaseColor.rgb;

				float3 verticalGradient = lerp(
					_BottomTint.rgb,
					_TopTint.rgb,
					saturate(uv.y)
				);

				color = lerp(
					color,
					verticalGradient,
					_GradientStrength
				);

				float centerGlow = SoftBlob(
					animatedUv,
					float2(0.52, 0.48),
					float2(_CenterGlowSize, _CenterGlowSize * 0.72)
				);

				color += _CenterGlowColor.rgb * centerGlow * _CenterGlowStrength;

				float leftGlow = SoftBlob(
					animatedUv,
					float2(-0.12, 0.18),
					float2(_LeftGlowSize * 0.62, _LeftGlowSize)
				);

				color += _LeftGlowColor.rgb * leftGlow * _LeftGlowStrength;

				float rightGlow = SoftBlob(
					animatedUv,
					float2(1.12, 0.62),
					float2(_RightGlowSize * 0.62, _RightGlowSize)
				);

				color += _RightGlowColor.rgb * rightGlow * _RightGlowStrength;

				float diagonal = dot(
					animatedUv - 0.5,
					normalize(float2(0.75, -0.38))
				);

				float band = 1.0 - smoothstep(
					0.0,
					_BandWidth,
					abs(diagonal - _BandOffset)
				);

				color += _BandColor.rgb * band * _BandStrength;

				float noise = Noise(
					animatedUv * _NoiseScale + _Time.y * _NoiseSpeed
				);

				color += (noise - 0.5) * _NoiseStrength;

				float2 centeredUv = uv - 0.5;
				float vignette = dot(centeredUv, centeredUv) * 2.0;
				vignette = smoothstep(0.18, 1.0, vignette);

				color *= 1.0 - vignette * _VignetteStrength;
				color *= _Brightness;

				return float4(color, _Alpha * input.color.a);
			}
			ENDCG
		}
	}
}