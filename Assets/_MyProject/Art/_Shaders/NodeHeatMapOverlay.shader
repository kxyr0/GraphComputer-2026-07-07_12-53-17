Shader "GraphComputer/UI/Node Heat Map Overlay"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

		[Header(Base)]
		_ColdColor ("Cold Color", Color) = (0.02, 0.1, 0.22, 1)
		_WarmColor ("Warm Color", Color) = (0.1, 0.85, 0.65, 1)
		_HotColor ("Hot Color", Color) = (1.0, 0.32, 0.08, 1)
		_Brightness ("Brightness", Range(0, 2)) = 1

		[Header(Heat)]
		_CoreIntensity ("Core Intensity", Range(0, 3)) = 1.35
		_ContourScale ("Contour Scale", Float) = 9
		_ContourStrength ("Contour Strength", Range(0, 1)) = 0.35
		_DriftSpeed ("Drift Speed", Float) = 0.35
		_Vignette ("Vignette", Range(0, 1)) = 0.55

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
			Name "Node Heat Map Overlay"

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

			float4 _ColdColor;
			float4 _WarmColor;
			float4 _HotColor;
			float _Brightness;
			float _CoreIntensity;
			float _ContourScale;
			float _ContourStrength;
			float _DriftSpeed;
			float _Vignette;

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

			float HeatBlob(float2 uv, float2 center, float radius)
			{
				float distanceToCenter = length(uv - center);
				return 1.0 - smoothstep(0.0, radius, distanceToCenter);
			}

			fixed4 frag(v2f input) : SV_Target
			{
				float2 uv = input.uv;
				fixed4 sprite = tex2D(_MainTex, uv) + _TextureSampleAdd;

				float time = _Time.y * _DriftSpeed;
				float2 centerA = float2(0.34 + sin(time) * 0.08, 0.58 + cos(time * 0.7) * 0.07);
				float2 centerB = float2(0.68 + cos(time * 0.9) * 0.07, 0.35 + sin(time * 0.8) * 0.06);
				float2 centerC = float2(0.5 + sin(time * 0.45) * 0.1, 0.76);

				float heat = 0.0;
				heat += HeatBlob(uv, centerA, 0.42) * 0.85;
				heat += HeatBlob(uv, centerB, 0.34) * 0.75;
				heat += HeatBlob(uv, centerC, 0.26) * 0.55;
				heat = saturate(heat * _CoreIntensity);

				float contourPhase = abs(frac(heat * _ContourScale) - 0.5);
				float contour = (1.0 - smoothstep(0.0, 0.08, contourPhase)) * _ContourStrength;
				float vignette = smoothstep(0.78, 0.18, length(uv - 0.5));

				float3 color = lerp(_ColdColor.rgb, _WarmColor.rgb, heat);
				color = lerp(color, _HotColor.rgb, smoothstep(0.62, 1.0, heat));
				color += _HotColor.rgb * contour;
				color *= lerp(1.0 - _Vignette, 1.0, vignette);
				color *= sprite.rgb * input.color.rgb * _Brightness;

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
