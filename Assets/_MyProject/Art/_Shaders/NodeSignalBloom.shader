Shader "GraphComputer/UI/Node Signal Bloom"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

		[Header(Base)]
		_BaseBoost ("Base Boost", Range(0, 2)) = 1
		_SignalColor ("Signal Color", Color) = (0.0, 1.0, 0.82, 1)
		_WarningColor ("Warning Color", Color) = (1.0, 0.42, 0.12, 1)

		[Header(Rings)]
		_RingStrength ("Ring Strength", Range(0, 2)) = 0.42
		_RingCount ("Ring Count", Range(1, 12)) = 4
		_RingSpeed ("Ring Speed", Float) = 0.45
		_RingWidth ("Ring Width", Range(0.005, 0.3)) = 0.08

		[Header(Brackets)]
		_BracketStrength ("Bracket Strength", Range(0, 2)) = 0.65
		_BracketSize ("Bracket Size", Range(0.02, 0.45)) = 0.18
		_BracketThickness ("Bracket Thickness", Range(0.002, 0.12)) = 0.025

		[Header(Load Flicker)]
		_FlickerStrength ("Flicker Strength", Range(0, 1)) = 0.12
		_FlickerSpeed ("Flicker Speed", Float) = 7

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
			Name "Node Signal Bloom"

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

			float _BaseBoost;
			float4 _SignalColor;
			float4 _WarningColor;
			float _RingStrength;
			float _RingCount;
			float _RingSpeed;
			float _RingWidth;
			float _BracketStrength;
			float _BracketSize;
			float _BracketThickness;
			float _FlickerStrength;
			float _FlickerSpeed;

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

			float RectLine(float2 uv, float2 minUv, float2 maxUv, float thickness)
			{
				float2 inside = step(minUv, uv) * step(uv, maxUv);
				float fill = inside.x * inside.y;
				float distanceToEdge = min(
					min(uv.x - minUv.x, maxUv.x - uv.x),
					min(uv.y - minUv.y, maxUv.y - uv.y)
				);

				return fill * (1.0 - smoothstep(0.0, thickness, distanceToEdge));
			}

			fixed4 frag(v2f input) : SV_Target
			{
				float2 uv = input.uv;
				fixed4 sprite = tex2D(_MainTex, uv) + _TextureSampleAdd;

				float2 centered = uv - 0.5;
				float radius = length(centered);
				float ringPhase = frac(radius * max(_RingCount, 1.0) - _Time.y * _RingSpeed);
				float ring = 1.0 - smoothstep(0.0, _RingWidth, ringPhase);
				ring *= 1.0 - smoothstep(0.42, 0.72, radius);

				float size = _BracketSize;
				float thick = _BracketThickness;
				float bracket =
					RectLine(uv, float2(0.0, 1.0 - size), float2(size, 1.0), thick) +
					RectLine(uv, float2(1.0 - size, 1.0 - size), float2(1.0, 1.0), thick) +
					RectLine(uv, float2(0.0, 0.0), float2(size, size), thick) +
					RectLine(uv, float2(1.0 - size, 0.0), float2(1.0, size), thick);

				float flicker = sin(_Time.y * _FlickerSpeed + uv.x * 17.0) * 0.5 + 0.5;
				float warning = smoothstep(0.58, 1.0, flicker) * _FlickerStrength;

				float3 color = sprite.rgb * input.color.rgb * _BaseBoost;
				color += _SignalColor.rgb * ring * _RingStrength;
				color += _SignalColor.rgb * saturate(bracket) * _BracketStrength;
				color = lerp(color, color + _WarningColor.rgb, warning);

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
