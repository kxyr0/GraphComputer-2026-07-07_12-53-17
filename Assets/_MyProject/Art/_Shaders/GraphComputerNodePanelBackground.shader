Shader "GraphComputer/UI/Node Panel Background"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

		[Header(Base)]
		_BaseBrightness ("Base Brightness", Range(0, 2)) = 1.0
		_SaturationBoost ("Saturation Boost", Range(0, 2)) = 1.0

		[Header(Gradient)]
		_GradientTopColor ("Gradient Top Color", Color) = (0.35, 0.75, 1.00, 1)
		_GradientBottomColor ("Gradient Bottom Color", Color) = (0.02, 0.04, 0.08, 1)
		_GradientStrength ("Gradient Strength", Range(0, 1)) = 0.22

		[Header(Accent Glow)]
		_AccentColor ("Accent Color", Color) = (0.10, 1.00, 0.95, 1)
		_AccentStrength ("Accent Strength", Range(0, 1)) = 0.12
		_RightSideGlow ("Right Side Glow", Range(0, 1)) = 0.18
		_CenterGlow ("Center Glow", Range(0, 1)) = 0.08

		[Header(Scanlines)]
		_ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.055
		_ScanlineScale ("Scanline Scale", Float) = 72
		_ScanlineSpeed ("Scanline Speed", Float) = 0.12

		[Header(Subtle Noise)]
		_NoiseStrength ("Noise Strength", Range(0, 0.2)) = 0.025
		_NoiseScale ("Noise Scale", Float) = 45
		_NoiseSpeed ("Noise Speed", Float) = 0.08

		[Header(Inner Shadow)]
		_InnerShadowStrength ("Inner Shadow Strength", Range(0, 1)) = 0.32
		_InnerShadowSize ("Inner Shadow Size", Range(0.001, 0.5)) = 0.14
		_VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.18

		[Header(UI)]
		[HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
		[HideInInspector] _Stencil ("Stencil ID", Float) = 0
		[HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
		[HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
		[HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
		[HideInInspector] _ColorMask ("Color Mask", Float) = 15
		[HideInInspector] _UseUIAlphaClip ("Use UI Alpha Clip", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
			Name "Node Panel Background"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
			#pragma multi_compile_local _ UNITY_UI_ALPHACLIP

			struct appdata_t
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;

			float _BaseBrightness;
			float _SaturationBoost;

			float4 _GradientTopColor;
			float4 _GradientBottomColor;
			float _GradientStrength;

			float4 _AccentColor;
			float _AccentStrength;
			float _RightSideGlow;
			float _CenterGlow;

			float _ScanlineStrength;
			float _ScanlineScale;
			float _ScanlineSpeed;

			float _NoiseStrength;
			float _NoiseScale;
			float _NoiseSpeed;

			float _InnerShadowStrength;
			float _InnerShadowSize;
			float _VignetteStrength;

			v2f vert(appdata_t input)
			{
				v2f output;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.worldPosition = input.vertex;
				output.vertex = UnityObjectToClipPos(input.vertex);
				output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
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

			float3 ApplySaturation(float3 color, float saturation)
			{
				float gray = dot(color, float3(0.299, 0.587, 0.114));
				return lerp(float3(gray, gray, gray), color, saturation);
			}

			fixed4 frag(v2f input) : SV_Target
			{
				float2 uv = input.uv;

				fixed4 sprite = tex2D(_MainTex, uv) + _TextureSampleAdd;

				float3 baseColor = sprite.rgb * input.color.rgb;
				baseColor *= _BaseBrightness;
				baseColor = ApplySaturation(baseColor, _SaturationBoost);

				float3 gradientColor = lerp(
					_GradientBottomColor.rgb,
					_GradientTopColor.rgb,
					saturate(uv.y)
				);

				float3 color = lerp(
					baseColor,
					baseColor + gradientColor * baseColor,
					_GradientStrength
				);

				float2 centerDelta = uv - float2(0.52, 0.48);
				float centerDistance = dot(centerDelta, centerDelta);
				float centerGlow = 1.0 - smoothstep(0.0, 0.52, centerDistance);

				color += _AccentColor.rgb * centerGlow * _CenterGlow;

				float rightGlow = pow(saturate(uv.x), 3.0);
				color += _AccentColor.rgb * rightGlow * _RightSideGlow * _AccentStrength;

				float scan = sin((uv.y + _Time.y * _ScanlineSpeed) * _ScanlineScale);
				scan = scan * 0.5 + 0.5;
				color *= 1.0 - (1.0 - scan) * _ScanlineStrength;

				float noise = Noise(uv * _NoiseScale + _Time.y * _NoiseSpeed);
				color += (noise - 0.5) * _NoiseStrength;

				float edgeDistance = min(
					min(uv.x, 1.0 - uv.x),
					min(uv.y, 1.0 - uv.y)
				);

				float innerShadow = 1.0 - smoothstep(
					0.0,
					_InnerShadowSize,
					edgeDistance
				);

				color *= 1.0 - innerShadow * _InnerShadowStrength;

				float2 vignetteUv = uv - 0.5;
				float vignette = dot(vignetteUv, vignetteUv) * 2.0;
				vignette = smoothstep(0.2, 1.0, vignette);

				color *= 1.0 - vignette * _VignetteStrength;

				fixed4 outputColor;
				outputColor.rgb = color;
				outputColor.a = sprite.a * input.color.a;

				#ifdef UNITY_UI_CLIP_RECT
				outputColor.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
				#endif

				#ifdef UNITY_UI_ALPHACLIP
				clip(outputColor.a - 0.001);
				#endif

				return outputColor;
			}
			ENDCG
		}
	}
}