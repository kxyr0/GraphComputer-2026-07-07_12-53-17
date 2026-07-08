Shader "GraphComputer/World/Panorama Background"
{
	Properties
	{
		[Header(Base Gradient)]
		_TopColor ("Top Color", Color) = (0.105, 0.110, 0.205, 1)
		_BottomColor ("Bottom Color", Color) = (0.035, 0.040, 0.095, 1)
		_HorizonColor ("Horizon Color", Color) = (0.050, 0.900, 1.000, 1)
		_HorizonStrength ("Horizon Strength", Range(0, 1)) = 0.18

		[Header(Nebula)]
		_NebulaAColor ("Nebula A", Color) = (0.050, 0.900, 1.000, 1)
		_NebulaBColor ("Nebula B", Color) = (0.850, 0.250, 1.000, 1)
		_NebulaStrength ("Nebula Strength", Range(0, 2)) = 0.65
		_NebulaScale ("Nebula Scale", Float) = 2.2
		_NebulaSpeed ("Nebula Speed", Float) = 0.035

		[Header(Stars)]
		_StarColor ("Star Color", Color) = (0.800, 0.950, 1.000, 1)
		_StarDensity ("Star Density", Range(0, 1)) = 0.055
		_StarBrightness ("Star Brightness", Range(0, 5)) = 1.6
		_StarTwinkleSpeed ("Star Twinkle Speed", Float) = 1.35

		[Header(Tech Lines)]
		_ScanlineColor ("Scanline Color", Color) = (0.100, 1.000, 0.950, 1)
		_ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.075
		_ScanlineScale ("Scanline Scale", Float) = 85
		_ScanlineSpeed ("Scanline Speed", Float) = 0.18

		[Header(Camera Parallax)]
		_CameraWorldPosition ("Camera World Position", Vector) = (0, 0, 0, 0)
		_Parallax ("Parallax", Float) = 0.018

		[Header(Final)]
		_VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.48
		_Brightness ("Brightness", Range(0, 3)) = 1.0
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
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			Name "Panorama Background"

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
				float3 worldPosition : TEXCOORD1;
				float4 color : COLOR;
			};

			float4 _TopColor;
			float4 _BottomColor;
			float4 _HorizonColor;
			float _HorizonStrength;

			float4 _NebulaAColor;
			float4 _NebulaBColor;
			float _NebulaStrength;
			float _NebulaScale;
			float _NebulaSpeed;

			float4 _StarColor;
			float _StarDensity;
			float _StarBrightness;
			float _StarTwinkleSpeed;

			float4 _ScanlineColor;
			float _ScanlineStrength;
			float _ScanlineScale;
			float _ScanlineSpeed;

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
				output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
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

				float ab = lerp(a, b, u.x);
				float cd = lerp(c, d, u.x);

				return lerp(ab, cd, u.y);
			}

			float Fbm(float2 p)
			{
				float value = 0.0;
				float amplitude = 0.5;

				value += Noise(p) * amplitude;
				p *= 2.03;
				amplitude *= 0.5;

				value += Noise(p) * amplitude;
				p *= 2.01;
				amplitude *= 0.5;

				value += Noise(p) * amplitude;
				p *= 2.07;
				amplitude *= 0.5;

				value += Noise(p) * amplitude;

				return value;
			}

			float StarLayer(float2 p, float density, float sizeMultiplier)
			{
				float2 cell = floor(p * density);
				float2 localPosition = frac(p * density) - 0.5;

				float randomValue = Hash21(cell);
				float enabled = step(1.0 - _StarDensity, randomValue);

				float radius = lerp(0.035, 0.080, Hash21(cell + 7.31)) * sizeMultiplier;
				float distanceToCenter = length(localPosition);

				float star = 1.0 - smoothstep(radius, radius * 1.8, distanceToCenter);

				float twinkle = sin(
					_Time.y * _StarTwinkleSpeed +
					randomValue * 37.0
				) * 0.5 + 0.5;

				twinkle = lerp(0.55, 1.25, twinkle);

				return star * enabled * twinkle;
			}

			float4 frag(v2f input) : SV_Target
			{
				float2 uv = input.uv;

				float2 cameraOffset = _CameraWorldPosition.xy * _Parallax;
				float2 panoramaPosition = uv * float2(1.8, 1.0) + cameraOffset;

				float3 background = lerp(
					_BottomColor.rgb,
					_TopColor.rgb,
					saturate(uv.y)
				);

				float horizon = 1.0 - abs(uv.y - 0.42) * 3.2;
				horizon = saturate(horizon);
				horizon = horizon * horizon;

				background = lerp(
					background,
					_HorizonColor.rgb,
					horizon * _HorizonStrength
				);

				float timeOffset = _Time.y * _NebulaSpeed;

				float2 nebulaPosition = panoramaPosition * _NebulaScale;
				nebulaPosition += float2(timeOffset, -timeOffset * 0.47);

				float warpA = Fbm(nebulaPosition * 0.73 + float2(8.3, 1.7));
				float warpB = Fbm(nebulaPosition * 0.91 + float2(-4.1, 6.8));
				float2 warp = float2(warpA, warpB) - 0.5;

				float nebula = Fbm(nebulaPosition + warp * 1.35);
				float cloud = smoothstep(0.38, 0.84, nebula);

				float colorMix = Fbm(nebulaPosition * 1.7 + float2(2.4, 9.1));
				float3 nebulaColor = lerp(
					_NebulaAColor.rgb,
					_NebulaBColor.rgb,
					colorMix
				);

				float3 finalColor = background;
				finalColor += nebulaColor * cloud * _NebulaStrength * 0.55;

				float starsA = StarLayer(panoramaPosition + float2(timeOffset * 0.3, 0.0), 42.0, 1.0);
				float starsB = StarLayer(panoramaPosition * 1.65 - float2(0.0, timeOffset * 0.4), 78.0, 0.7);

				float stars = saturate(starsA + starsB * 0.75);
				finalColor += _StarColor.rgb * stars * _StarBrightness;

				float scan = sin((uv.y + _Time.y * _ScanlineSpeed) * _ScanlineScale);
				scan = scan * 0.5 + 0.5;
				scan = pow(scan, 22.0);

				finalColor += _ScanlineColor.rgb * scan * _ScanlineStrength;

				float diagonalSweep = sin(
					(uv.x + uv.y) * 5.0 +
					_Time.y * 0.32
				) * 0.5 + 0.5;

				diagonalSweep = pow(diagonalSweep, 9.0);
				finalColor += _HorizonColor.rgb * diagonalSweep * 0.035;

				float2 centeredUv = uv - 0.5;
				float vignette = dot(centeredUv, centeredUv) * 2.15;
				vignette = smoothstep(0.18, 1.05, vignette);

				finalColor *= 1.0 - vignette * _VignetteStrength;
				finalColor *= _Brightness;

				return float4(finalColor, _Alpha * input.color.a);
			}
			ENDCG
		}
	}
}