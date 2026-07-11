Shader "GraphComputer/UI/Packet Trace Flow"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

		[Header(Base)]
		_BaseTint ("Base Tint", Color) = (0.02, 0.035, 0.055, 1)
		_TraceColor ("Trace Color", Color) = (0.18, 0.68, 1.0, 1)
		_PacketColor ("Packet Color", Color) = (0.9, 1.0, 0.36, 1)
		_Brightness ("Brightness", Range(0, 2)) = 1

		[Header(Trace)]
		_TraceScale ("Trace Scale", Float) = 10
		_TraceWidth ("Trace Width", Range(0.002, 0.18)) = 0.04
		_PacketSpeed ("Packet Speed", Float) = 1.4
		_PacketLength ("Packet Length", Range(0.01, 0.5)) = 0.13
		_PacketStrength ("Packet Strength", Range(0, 3)) = 1.3
		_NodeStrength ("Node Strength", Range(0, 2)) = 0.65

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
			Name "Packet Trace Flow"

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
			float4 _TraceColor;
			float4 _PacketColor;
			float _Brightness;
			float _TraceScale;
			float _TraceWidth;
			float _PacketSpeed;
			float _PacketLength;
			float _PacketStrength;
			float _NodeStrength;

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

			float TraceLine(float value, float width)
			{
				float centered = abs(frac(value) - 0.5);
				return 1.0 - smoothstep(width, width + 0.035, centered);
			}

			fixed4 frag(v2f input) : SV_Target
			{
				float2 uv = input.uv;
				fixed4 sprite = tex2D(_MainTex, uv) + _TextureSampleAdd;

				float wave = sin(uv.x * 7.0 + _Time.y * 0.55) * 0.12;
				float horizontal = TraceLine(uv.y * _TraceScale + wave, _TraceWidth);
				float vertical = TraceLine(uv.x * (_TraceScale * 0.65) + sin(uv.y * 5.0) * 0.08, _TraceWidth * 0.75);
				float trace = saturate(horizontal + vertical * 0.7);

				float packetPhaseH = frac(uv.x * 2.6 + uv.y * 0.35 - _Time.y * _PacketSpeed);
				float packetPhaseV = frac(uv.y * 2.2 - uv.x * 0.25 - _Time.y * (_PacketSpeed * 0.8));
				float packetH = (1.0 - smoothstep(0.0, _PacketLength, packetPhaseH)) * horizontal;
				float packetV = (1.0 - smoothstep(0.0, _PacketLength, packetPhaseV)) * vertical;
				float packet = saturate(packetH + packetV);

				float2 nodeUv = frac(uv * float2(_TraceScale * 0.65, _TraceScale)) - 0.5;
				float node = 1.0 - smoothstep(0.035, 0.12, length(nodeUv));
				node *= trace;

				float3 color = _BaseTint.rgb * sprite.rgb;
				color += _TraceColor.rgb * trace * 0.55;
				color += _TraceColor.rgb * node * _NodeStrength;
				color += _PacketColor.rgb * packet * _PacketStrength;
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
