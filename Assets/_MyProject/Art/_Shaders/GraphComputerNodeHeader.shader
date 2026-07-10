Shader "GraphComputer/UI/Node Header"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

		[Header(Base)]
		_BaseBrightness ("Base Brightness", Range(0, 2)) = 1.0

		[Header(Top Rim)]
		_TopRimColor ("Top Rim Color", Color) = (0.45, 0.85, 1.0, 1)
		_TopRimStrength ("Top Rim Strength", Range(0, 1)) = 0.16
		_TopRimSize ("Top Rim Size", Range(0.001, 0.25)) = 0.055

		[Header(Gloss)]
		_GlossColor ("Gloss Color", Color) = (0.75, 0.95, 1.0, 1)
		_GlossStrength ("Gloss Strength", Range(0, 1)) = 0.10
		_GlossPosition ("Gloss Position", Range(0, 1)) = 0.68
		_GlossWidth ("Gloss Width", Range(0.001, 0.8)) = 0.22

		[Header(Active Glow)]
		_ActiveGlowColor ("Active Glow Color", Color) = (0.10, 1.00, 0.95, 1)
		_ActiveGlowStrength ("Active Glow Strength", Range(0, 1)) = 0.08
		_ActivePulseStrength ("Active Pulse Strength", Range(0, 1)) = 0.035
		_ActivePulseSpeed ("Active Pulse Speed", Float) = 1.2

		[Header(Depth)]
		_BottomShadowStrength ("Bottom Shadow Strength", Range(0, 1)) = 0.22
		_SideShadowStrength ("Side Shadow Strength", Range(0, 1)) = 0.12

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
			Name "Node Header"

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

			float4 _TopRimColor;
			float _TopRimStrength;
			float _TopRimSize;

			float4 _GlossColor;
			float _GlossStrength;
			float _GlossPosition;
			float _GlossWidth;

			float4 _ActiveGlowColor;
			float _ActiveGlowStrength;
			float _ActivePulseStrength;
			float _ActivePulseSpeed;

			float _BottomShadowStrength;
			float _SideShadowStrength;

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

			fixed4 frag(v2f input) : SV_Target
			{
				float2 uv = input.uv;

				fixed4 sprite = tex2D(_MainTex, uv) + _TextureSampleAdd;

				float3 color = sprite.rgb * input.color.rgb * _BaseBrightness;

				float topRim = 1.0 - smoothstep(
					0.0,
					_TopRimSize,
					1.0 - uv.y
				);

				color += _TopRimColor.rgb * topRim * _TopRimStrength;

				float glossDistance = abs(uv.y - _GlossPosition);
				float gloss = 1.0 - smoothstep(
					0.0,
					_GlossWidth,
					glossDistance
				);

				float glossMask = smoothstep(0.0, 0.18, uv.x) *
				                 smoothstep(1.0, 0.72, uv.x);

				color += _GlossColor.rgb * gloss * glossMask * _GlossStrength;

				float pulse = sin(_Time.y * _ActivePulseSpeed) * 0.5 + 0.5;
				float activeGlow = _ActiveGlowStrength +
				                   pulse * _ActivePulseStrength;

				float horizontalGlow = 1.0 - abs(uv.x - 0.5) * 2.0;
				horizontalGlow = saturate(horizontalGlow);
				horizontalGlow = horizontalGlow * horizontalGlow;

				color += _ActiveGlowColor.rgb * horizontalGlow * activeGlow;

				float bottomShadow = smoothstep(0.58, 0.0, uv.y);
				color *= 1.0 - bottomShadow * _BottomShadowStrength;

				float sideDistance = min(uv.x, 1.0 - uv.x);
				float sideShadow = 1.0 - smoothstep(0.0, 0.18, sideDistance);
				color *= 1.0 - sideShadow * _SideShadowStrength;

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