Shader "GraphComputer/UI/Research Progress Compiler"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

		[Header(Base)]
		_BaseTint ("Base Tint", Color) = (0.02, 0.08, 0.13, 1)
		_CoreColor ("Core Color", Color) = (0.0, 0.62, 1.0, 1)
		_SparkColor ("Spark Color", Color) = (0.8, 1.0, 0.36, 1)
		_Activity ("Activity", Range(0, 1)) = 1

		[Header(Progress)]
		_Progress ("Progress", Range(0, 1)) = 0.65
		_ProgressSoftness ("Progress Softness", Range(0.001, 0.2)) = 0.035
		_EdgeGlow ("Progress Edge Glow", Range(0, 2)) = 0.75

		[Header(Data Flow)]
		_FlowSpeed ("Flow Speed", Float) = 0.85
		_PacketCount ("Packet Count", Range(1, 18)) = 9
		_PacketWidth ("Packet Width", Range(0.005, 0.24)) = 0.055
		_CircuitScale ("Circuit Scale", Float) = 10
		_CircuitStrength ("Circuit Strength", Range(0, 1)) = 0.32

		[Header(Scan)]
		_ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.08
		_ScanlineScale ("Scanline Scale", Float) = 96
		_NoiseStrength ("Noise Strength", Range(0, 0.25)) = 0.035

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
			Name "Research Progress Compiler"

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
			float4 _CoreColor;
			float4 _SparkColor;
			float _Activity;

			float _Progress;
			float _ProgressSoftness;
			float _EdgeGlow;

			float _FlowSpeed;
			float _PacketCount;
			float _PacketWidth;
			float _CircuitScale;
			float _CircuitStrength;

			float _ScanlineStrength;
			float _ScanlineScale;
			float _NoiseStrength;

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

			fixed4 frag(v2f input) : SV_Target
			{
				float2 uv = input.uv;
				fixed4 sprite = tex2D(_MainTex, uv) + _TextureSampleAdd;

				float progress = saturate(_Progress);
				float fill = 1.0 - smoothstep(progress, progress + _ProgressSoftness, uv.x);
				float edge = 1.0 - smoothstep(0.0, _ProgressSoftness * 2.0, abs(uv.x - progress));

				float2 gridUv = uv * max(_CircuitScale, 0.001);
				float2 cell = abs(frac(gridUv) - 0.5);
				float circuit = 1.0 - smoothstep(0.0, 0.045, min(cell.x, cell.y));
				float gate = step(0.64, Hash21(floor(gridUv)));
				circuit *= lerp(0.35, 1.0, gate);

				float lane = frac((uv.x + uv.y * 0.32) * max(_PacketCount, 1.0) - _Time.y * _FlowSpeed);
				float packet = 1.0 - smoothstep(0.0, _PacketWidth, lane);
				packet *= fill;

				float scan = sin((uv.y + _Time.y * 0.12) * _ScanlineScale) * 0.5 + 0.5;
				float noise = Hash21(floor((uv + _Time.y * 0.03) * 160.0));

				float3 tintedBase = lerp(_BaseTint.rgb, input.color.rgb, 0.38);
				float3 color = tintedBase * sprite.rgb;
				color += _CoreColor.rgb * circuit * _CircuitStrength * fill * _Activity;
				color += _CoreColor.rgb * edge * _EdgeGlow * _Activity;
				color += _SparkColor.rgb * packet * _Activity;
				color *= 1.0 - (1.0 - scan) * _ScanlineStrength;
				color += (noise - 0.5) * _NoiseStrength;

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
