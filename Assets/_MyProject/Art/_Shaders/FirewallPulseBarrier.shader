Shader "GraphComputer/UI/Firewall Pulse Barrier"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

		[Header(Base)]
		_BaseTint ("Base Tint", Color) = (0.025, 0.055, 0.075, 1)
		_GridColor ("Grid Color", Color) = (0.12, 0.95, 0.78, 1)
		_PulseColor ("Pulse Color", Color) = (1.0, 0.38, 0.12, 1)
		_Brightness ("Brightness", Range(0, 2)) = 1

		[Header(Barrier)]
		_HexScale ("Hex Scale", Float) = 12
		_LineWidth ("Line Width", Range(0.002, 0.12)) = 0.028
		_PulseSpeed ("Pulse Speed", Float) = 0.7
		_PulseStrength ("Pulse Strength", Range(0, 2)) = 0.9
		_EdgeGlow ("Edge Glow", Range(0, 2)) = 0.55

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
			Name "Firewall Pulse Barrier"

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

			float4 _BaseTint;
			float4 _GridColor;
			float4 _PulseColor;
			float _Brightness;
			float _HexScale;
			float _LineWidth;
			float _PulseSpeed;
			float _PulseStrength;
			float _EdgeGlow;

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

			float PeriodicLine(float value, float width)
			{
				float phase = frac(value);
				float distanceToLine = min(phase, 1.0 - phase);
				return 1.0 - smoothstep(0.0, width, distanceToLine);
			}

			fixed4 frag(v2f input) : SV_Target
			{
				float2 uv = input.uv;
				fixed4 sprite = tex2D(_MainTex, uv) + _TextureSampleAdd;

				float2 centered = uv - 0.5;
				float2 gridUv = centered * _HexScale;
				float lineA = PeriodicLine(gridUv.x, _LineWidth);
				float lineB = PeriodicLine(dot(gridUv, float2(0.5, 0.8660254)), _LineWidth);
				float lineC = PeriodicLine(dot(gridUv, float2(-0.5, 0.8660254)), _LineWidth);
				float grid = saturate(lineA + lineB + lineC);

				float radius = length(centered);
				float ringPhase = frac(radius * 4.0 - _Time.y * _PulseSpeed);
				float ring = 1.0 - smoothstep(0.0, 0.08, abs(ringPhase - 0.5));
				ring *= 1.0 - smoothstep(0.52, 0.72, radius);

				float edgeDistance = 0.5 - max(abs(centered.x), abs(centered.y));
				float edge = 1.0 - smoothstep(0.0, 0.18, edgeDistance);

				float3 color = _BaseTint.rgb * sprite.rgb;
				color += _GridColor.rgb * grid * 0.65;
				color += _PulseColor.rgb * ring * _PulseStrength;
				color += _GridColor.rgb * edge * _EdgeGlow;
				color *= input.color.rgb * _Brightness;

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
