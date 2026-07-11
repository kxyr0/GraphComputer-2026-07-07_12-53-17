Shader "GraphComputer/UI/Neural Circuit Aura"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

		[Header(Base)]
		_BaseTint ("Base Tint", Color) = (0.025, 0.035, 0.07, 1)
		_NodeColor ("Node Color", Color) = (0.18, 1.0, 0.68, 1)
		_LinkColor ("Link Color", Color) = (0.34, 0.56, 1.0, 1)
		_AuraColor ("Aura Color", Color) = (0.95, 0.22, 1.0, 1)
		_Brightness ("Brightness", Range(0, 2)) = 1

		[Header(Circuit)]
		_CellScale ("Cell Scale", Float) = 8
		_FlowSpeed ("Flow Speed", Float) = 0.75
		_NodeSize ("Node Size", Range(0.01, 0.35)) = 0.12
		_LinkWidth ("Link Width", Range(0.002, 0.12)) = 0.035
		_PulseStrength ("Pulse Strength", Range(0, 2)) = 0.9
		_AuraStrength ("Aura Strength", Range(0, 2)) = 0.55

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
			Name "Neural Circuit Aura"

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
			float4 _NodeColor;
			float4 _LinkColor;
			float4 _AuraColor;
			float _Brightness;
			float _CellScale;
			float _FlowSpeed;
			float _NodeSize;
			float _LinkWidth;
			float _PulseStrength;
			float _AuraStrength;

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
				p = frac(p * float2(234.34, 435.21));
				p += dot(p, p + 34.23);
				return frac(p.x * p.y);
			}

			fixed4 frag(v2f input) : SV_Target
			{
				float2 uv = input.uv;
				fixed4 sprite = tex2D(_MainTex, uv) + _TextureSampleAdd;

				float2 grid = uv * _CellScale;
				float2 cell = floor(grid);
				float2 local = frac(grid) - 0.5;

				float randomA = Hash21(cell);
				float randomB = Hash21(cell + 17.0);
				float horizontalMask = step(0.25, randomA);
				float verticalMask = step(0.38, randomB);

				float node = 1.0 - smoothstep(_NodeSize, _NodeSize + 0.06, length(local));
				float horizontal = (1.0 - smoothstep(_LinkWidth, _LinkWidth + 0.025, abs(local.y))) * horizontalMask;
				float vertical = (1.0 - smoothstep(_LinkWidth, _LinkWidth + 0.025, abs(local.x))) * verticalMask;
				float link = saturate(horizontal + vertical);

				float flow = sin((grid.x + grid.y) * 2.6 - _Time.y * _FlowSpeed * 6.0) * 0.5 + 0.5;
				float pulse = smoothstep(0.72, 1.0, flow) * link;
				float aura = 1.0 - smoothstep(0.0, 0.72, length(uv - 0.5));

				float3 color = _BaseTint.rgb * sprite.rgb;
				color += _LinkColor.rgb * link * 0.55;
				color += _NodeColor.rgb * node;
				color += _NodeColor.rgb * pulse * _PulseStrength;
				color += _AuraColor.rgb * aura * _AuraStrength;
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
