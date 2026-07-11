Shader "GraphComputer/UI/Link Overdrive"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

		[Header(Base)]
		_Color ("Base Color", Color) = (0.05, 0.86, 1.0, 1)
		_ArcColor ("Arc Color", Color) = (1.0, 0.86, 0.22, 1)
		_CoreAlpha ("Core Alpha", Range(0, 1)) = 0.8
		_Thickness ("Thickness", Range(0.02, 1)) = 0.72

		[Header(Packets)]
		_Speed ("Speed", Float) = 1.1
		_PacketCount ("Packet Count", Range(1, 16)) = 5
		_PacketSize ("Packet Size", Range(0.005, 0.25)) = 0.055
		_TrailLength ("Trail Length", Range(0.0, 1.0)) = 0.22

		[Header(Voltage)]
		_ArcStrength ("Arc Strength", Range(0, 4)) = 1.35
		_JitterStrength ("Jitter Strength", Range(0, 1)) = 0.18
		_EndFade ("End Fade", Range(0, 0.5)) = 0.05

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
			Name "Link Overdrive"

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
			float4 _ClipRect;
			float4 _Color;
			float4 _ArcColor;
			float _CoreAlpha;
			float _Thickness;
			float _Speed;
			float _PacketCount;
			float _PacketSize;
			float _TrailLength;
			float _ArcStrength;
			float _JitterStrength;
			float _EndFade;

			v2f vert(appdata_t input)
			{
				v2f output;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.worldPosition = input.vertex;
				output.vertex = UnityObjectToClipPos(output.worldPosition);
				output.uv = input.texcoord;
				output.color = input.color * _Color;

				return output;
			}

			float Hash11(float p)
			{
				return frac(sin(p * 17.17) * 43758.5453);
			}

			fixed4 frag(v2f input) : SV_Target
			{
				fixed4 tex = tex2D(_MainTex, input.uv);
				float x = saturate(input.uv.x);
				float y = saturate(input.uv.y);

				float sideDistance = abs(y - 0.5) * 2.0;
				float jitter = (Hash11(floor(x * 32.0 + _Time.y * 18.0)) - 0.5) * _JitterStrength;
				float electricCenter = 0.5 + jitter * 0.18;
				float electricDistance = abs(y - electricCenter) * 2.0;

				float core = 1.0 - smoothstep(_Thickness, 1.0, sideDistance);
				float arcLine = 1.0 - smoothstep(0.0, 0.24, electricDistance);

				float phase = frac(_Time.y * _Speed);
				float packetSpace = frac(x * max(_PacketCount, 1.0) - phase);
				float packet = 1.0 - smoothstep(0.0, _PacketSize, packetSpace);
				float trail = 1.0 - smoothstep(0.0, max(_TrailLength, 0.0001), packetSpace);

				float endFade =
					smoothstep(0.0, max(_EndFade, 0.0001), x) *
					smoothstep(0.0, max(_EndFade, 0.0001), 1.0 - x);

				packet *= endFade;
				trail *= endFade;

				float3 color = input.color.rgb * core;
				color += _ArcColor.rgb * (packet + trail * 0.35) * _ArcStrength;
				color += _ArcColor.rgb * arcLine * packet * _ArcStrength;

				fixed4 outputColor;
				outputColor.rgb = color;
				outputColor.a = max(core * _CoreAlpha * tex.a, packet + trail * 0.35) * input.color.a;

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
