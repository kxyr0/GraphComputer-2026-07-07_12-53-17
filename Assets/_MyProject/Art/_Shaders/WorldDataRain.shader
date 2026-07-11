Shader "GraphComputer/World/Data Rain Overlay"
{
	Properties
	{
		[Header(Base)]
		_BackgroundColor ("Background Color", Color) = (0.02, 0.025, 0.05, 1)
		_RainColor ("Rain Color", Color) = (0.0, 1.0, 0.72, 1)
		_SecondaryColor ("Secondary Color", Color) = (0.24, 0.52, 1.0, 1)
		_Alpha ("Alpha", Range(0, 1)) = 0.42

		[Header(Rain)]
		_ColumnCount ("Column Count", Range(8, 180)) = 64
		_FallSpeed ("Fall Speed", Float) = 0.28
		_TrailLength ("Trail Length", Range(0.02, 1)) = 0.34
		_CellDensity ("Cell Density", Range(4, 80)) = 32
		_BlinkSpeed ("Blink Speed", Float) = 9

		[Header(Depth)]
		_VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.45
		_CenterClear ("Center Clear", Range(0, 1)) = 0.22
		_Brightness ("Brightness", Range(0, 3)) = 1
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent-50"
			"RenderType" = "Transparent"
			"IgnoreProjector" = "True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			Name "Data Rain Overlay"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			struct v2f
			{
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			float4 _BackgroundColor;
			float4 _RainColor;
			float4 _SecondaryColor;
			float _Alpha;
			float _ColumnCount;
			float _FallSpeed;
			float _TrailLength;
			float _CellDensity;
			float _BlinkSpeed;
			float _VignetteStrength;
			float _CenterClear;
			float _Brightness;

			v2f vert(appdata input)
			{
				v2f output;
				output.position = UnityObjectToClipPos(input.vertex);
				output.uv = input.uv;
				output.color = input.color;
				return output;
			}

			float Hash21(float2 p)
			{
				p = frac(p * float2(123.34, 456.21));
				p += dot(p, p + 45.32);
				return frac(p.x * p.y);
			}

			float ColumnRain(float2 uv)
			{
				float column = floor(uv.x * _ColumnCount);
				float columnSeed = Hash21(float2(column, 17.0));
				float speed = _FallSpeed * lerp(0.65, 1.85, columnSeed);
				float head = frac(columnSeed + _Time.y * speed);
				float wrappedDistance = frac(head - uv.y);
				float trail = 1.0 - smoothstep(0.0, _TrailLength, wrappedDistance);

				float cell = floor(uv.y * _CellDensity);
				float blink = Hash21(float2(column, cell) + floor(_Time.y * _BlinkSpeed));
				float glyph = step(0.42, blink);

				float columnWidth = abs(frac(uv.x * _ColumnCount) - 0.5) * 2.0;
				float thinColumn = 1.0 - smoothstep(0.28, 0.52, columnWidth);

				return trail * glyph * thinColumn;
			}

			float4 frag(v2f input) : SV_Target
			{
				float2 uv = input.uv;
				float rain = ColumnRain(uv);

				float2 centered = uv - 0.5;
				float centerDistance = dot(centered, centered) * 2.0;
				float vignette = smoothstep(0.18, 1.0, centerDistance);
				float centerClear = 1.0 - smoothstep(0.0, 0.55, centerDistance);

				float3 rainColor = lerp(_SecondaryColor.rgb, _RainColor.rgb, rain);
				float3 color = _BackgroundColor.rgb;
				color += rainColor * rain * _Brightness;
				color *= 1.0 - vignette * _VignetteStrength;

				float alpha = _Alpha * input.color.a;
				alpha *= 1.0 - centerClear * _CenterClear;
				alpha = max(alpha * 0.18, rain * _Alpha);

				return float4(color, saturate(alpha));
			}
			ENDCG
		}
	}
}
