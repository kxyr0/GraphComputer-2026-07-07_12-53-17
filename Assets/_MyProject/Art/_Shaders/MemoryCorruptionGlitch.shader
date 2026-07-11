Shader "GraphComputer/UI/Memory Corruption Glitch"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

		[Header(Base)]
		_BaseTint ("Base Tint", Color) = (0.045, 0.06, 0.08, 1)
		_ErrorColor ("Error Color", Color) = (1.0, 0.08, 0.2, 1)
		_GhostColor ("Ghost Color", Color) = (0.0, 0.95, 1.0, 1)
		_Brightness ("Brightness", Range(0, 2)) = 1

		[Header(Glitch)]
		_ShiftStrength ("Shift Strength", Range(0, 0.1)) = 0.018
		_BlockCount ("Block Count", Float) = 18
		_GlitchSpeed ("Glitch Speed", Float) = 7
		_TearStrength ("Tear Strength", Range(0, 1)) = 0.32
		_NoiseStrength ("Noise Strength", Range(0, 1)) = 0.18

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
			Name "Memory Corruption Glitch"

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
			float4 _ErrorColor;
			float4 _GhostColor;
			float _Brightness;
			float _ShiftStrength;
			float _BlockCount;
			float _GlitchSpeed;
			float _TearStrength;
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
				float timeStep = floor(_Time.y * _GlitchSpeed);
				float row = floor(uv.y * _BlockCount);
				float blockNoise = Hash21(float2(row, timeStep));
				float tearMask = step(1.0 - _TearStrength, blockNoise);
				float offset = (blockNoise - 0.5) * _ShiftStrength * tearMask;

				float2 shiftedUv = uv + float2(offset, 0.0);
				fixed4 baseSample = tex2D(_MainTex, shiftedUv) + _TextureSampleAdd;
				fixed4 redSample = tex2D(_MainTex, shiftedUv + float2(_ShiftStrength, 0.0)) + _TextureSampleAdd;
				fixed4 cyanSample = tex2D(_MainTex, shiftedUv - float2(_ShiftStrength, 0.0)) + _TextureSampleAdd;

				float2 blockUv = floor(uv * _BlockCount);
				float block = Hash21(blockUv + timeStep);
				float digitalNoise = Hash21(floor((uv + _Time.y * 0.02) * 180.0));
				float errorBlock = step(0.86, block) * tearMask;
				float scan = step(0.94, frac(uv.y * 90.0 + _Time.y * 16.0));

				float3 color;
				color.r = redSample.r;
				color.g = baseSample.g;
				color.b = cyanSample.b;
				color = lerp(_BaseTint.rgb, color, 0.68);
				color += _GhostColor.rgb * abs(offset) * 12.0;
				color += _ErrorColor.rgb * errorBlock;
				color += _ErrorColor.rgb * scan * _NoiseStrength;
				color += (digitalNoise - 0.5) * _NoiseStrength;
				color *= input.color.rgb * _Brightness;

				fixed4 outputColor;
				outputColor.rgb = color;
				outputColor.a = baseSample.a * input.color.a;

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
