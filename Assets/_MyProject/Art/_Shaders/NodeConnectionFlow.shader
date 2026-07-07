Shader "UI/NodeConnectionFlow"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Base Color", Color) = (0.1, 1, 0.95, 1)

		_PulseColor ("Pulse Color", Color) = (1, 0.75, 0.05, 1)
		_FlowSpeed ("Flow Speed", Float) = 0.55
		_PulseSize ("Pulse Size", Range(0.005, 0.25)) = 0.045
		_PulseSoftness ("Pulse Softness", Range(0.01, 1)) = 0.35
		_PulseIntensity ("Pulse Intensity", Range(0, 5)) = 2.4

		_TrailLength ("Trail Length", Range(0, 1)) = 0.18
		_TrailIntensity ("Trail Intensity", Range(0, 2)) = 0.55

		_BaseAlpha ("Base Alpha", Range(0, 1)) = 1

		[HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
		[HideInInspector] _Stencil ("Stencil ID", Float) = 0
		[HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
		[HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
		[HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
		[HideInInspector] _ColorMask ("Color Mask", Float) = 15
		[HideInInspector] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
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
			Name "Default"

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
				fixed4 color : COLOR;
				float2 uv : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			fixed4 _Color;
			fixed4 _PulseColor;

			float _FlowSpeed;
			float _PulseSize;
			float _PulseSoftness;
			float _PulseIntensity;

			float _TrailLength;
			float _TrailIntensity;

			float _BaseAlpha;
			float4 _ClipRect;

			v2f vert(appdata_t v)
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

				OUT.worldPosition = v.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
				OUT.uv = v.texcoord;
				OUT.color = v.color * _Color;

				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 color = IN.color * tex2D(_MainTex, IN.uv);

				float lineProgress = saturate(IN.uv.x);
				float side = saturate(IN.uv.y);

				float phase = frac(_Time.y * _FlowSpeed);

				float dx = abs(lineProgress - phase);
				dx = min(dx, 1.0 - dx);

				float dy = abs(side - 0.5) * 2.0;

				float normalizedX = dx / max(_PulseSize, 0.0001);
				float circleDistance = length(float2(normalizedX, dy));

				float dot = 1.0 - smoothstep(_PulseSoftness, 1.0, circleDistance);

				float behind = frac(phase - lineProgress);
				float trail = 1.0 - smoothstep(0.0, max(_TrailLength, 0.0001), behind);
				trail *= 1.0 - saturate(dy * 0.65);
				trail *= _TrailIntensity;

				float flow = saturate(max(dot, trail));

				color.rgb = lerp(color.rgb, _PulseColor.rgb, saturate(flow * _PulseIntensity));
				color.a *= _BaseAlpha;
				color.a = max(color.a, _PulseColor.a * dot * 0.9);

				#ifdef UNITY_UI_CLIP_RECT
				color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
				#endif

				#ifdef UNITY_UI_ALPHACLIP
				clip(color.a - 0.001);
				#endif

				return color;
			}
			ENDCG
		}
	}
}