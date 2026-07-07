Shader "UI/NodeConnectionFlow"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

		_Color ("Base Color", Color) = (0.1, 1, 0.95, 1)
		_BaseAlpha ("Base Alpha", Range(0, 1)) = 0.85
		_BaseIntensity ("Base Intensity", Range(0, 3)) = 1

		_PulseColor ("Pulse Color", Color) = (1, 0.78, 0.08, 1)
		_PulseIntensity ("Pulse Intensity", Range(0, 8)) = 2.8

		_FlowSpeed ("Flow Speed", Float) = 0.55
		_FlowOffset ("Flow Offset", Range(0, 1)) = 0
		_FlowDirection ("Flow Direction", Range(-1, 1)) = 1

		_PulseCount ("Pulse Count", Range(1, 8)) = 1
		_PulseSize ("Pulse Size", Range(0.005, 0.25)) = 0.045
		_PulseSoftness ("Pulse Softness", Range(0.01, 1)) = 0.35

		_TrailLength ("Trail Length", Range(0, 1)) = 0.18
		_TrailIntensity ("Trail Intensity", Range(0, 3)) = 0.65

		_EdgeSoftness ("Edge Softness", Range(0.001, 0.5)) = 0.18
		_EndFade ("End Fade", Range(0, 0.5)) = 0.04

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

			float _BaseAlpha;
			float _BaseIntensity;

			float _PulseIntensity;
			float _FlowSpeed;
			float _FlowOffset;
			float _FlowDirection;

			float _PulseCount;
			float _PulseSize;
			float _PulseSoftness;

			float _TrailLength;
			float _TrailIntensity;

			float _EdgeSoftness;
			float _EndFade;

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
				fixed4 tex = tex2D(_MainTex, IN.uv);
				fixed4 baseColor = IN.color * tex;

				float rawProgress = saturate(IN.uv.x);
				float side = saturate(IN.uv.y);

				float direction01 = step(0.0, _FlowDirection);
				float progress = lerp(1.0 - rawProgress, rawProgress, direction01);

				float sideDistance = abs(side - 0.5) * 2.0;

				float edgeSoftness = max(_EdgeSoftness, 0.001);
				float lineAlpha = 1.0 - smoothstep(1.0 - edgeSoftness, 1.0, sideDistance);

				float endFadeSize = max(_EndFade, 0.0001);
				float endFade =
					smoothstep(0.0, endFadeSize, rawProgress) *
					smoothstep(0.0, endFadeSize, 1.0 - rawProgress);

				float pulseCount = max(_PulseCount, 1.0);
				float pulseSpace = frac(progress * pulseCount);

				float phase = frac(_Time.y * _FlowSpeed + _FlowOffset);

				float dx = abs(pulseSpace - phase);
				dx = min(dx, 1.0 - dx);

				float normalizedX = dx / max(_PulseSize, 0.0001);
				float circleDistance = length(float2(normalizedX, sideDistance));

				float pulse =
					1.0 - smoothstep(_PulseSoftness, 1.0, circleDistance);

				pulse *= endFade;

				float behind = frac(phase - pulseSpace);

				float trail = 1.0 - smoothstep(
					0.0,
					max(_TrailLength, 0.0001),
					behind
				);

				trail *= 1.0 - saturate(sideDistance * 0.75);
				trail *= endFade;

				float flowAmount = saturate(
					pulse * _PulseIntensity +
					trail * _TrailIntensity
				);

				fixed4 finalColor = baseColor;

				finalColor.rgb *= _BaseIntensity;
				finalColor.rgb += _PulseColor.rgb * flowAmount;

				float baseAlpha = baseColor.a * _BaseAlpha * lineAlpha;
				float pulseAlpha = _PulseColor.a * saturate(pulse + trail * 0.45);

				finalColor.a = max(baseAlpha, pulseAlpha);

				#ifdef UNITY_UI_CLIP_RECT
				finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
				#endif

				#ifdef UNITY_UI_ALPHACLIP
				clip(finalColor.a - 0.001);
				#endif

				return finalColor;
			}
			ENDCG
		}
	}
}