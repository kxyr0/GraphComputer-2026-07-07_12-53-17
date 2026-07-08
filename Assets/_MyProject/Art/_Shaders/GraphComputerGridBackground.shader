Shader "GraphComputer/World/Infinite Grid Lines"
{
	Properties
	{
		_TintColor ("Tint Color", Color) = (1, 1, 1, 1)
		_LineAlpha ("Line Alpha", Range(0, 2)) = 1
		_Intensity ("Intensity", Range(0, 5)) = 1.25

		[Header(Camera Fade)]
		_DistanceFade ("Distance Fade", Range(0, 1)) = 0.25
		_FadeStart ("Fade Start", Float) = 25
		_FadeEnd ("Fade End", Float) = 70

		[Header(Subtle Motion)]
		_PulseStrength ("Pulse Strength", Range(0, 1)) = 0.035
		_PulseScale ("Pulse Scale", Float) = 0.18
		_PulseSpeed ("Pulse Speed", Float) = 0.65
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
			"IgnoreProjector" = "True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			Name "Infinite Grid Lines"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
			};

			struct v2f
			{
				float4 position : SV_POSITION;
				float4 color : COLOR;
				float3 worldPosition : TEXCOORD0;
			};

			float4 _TintColor;
			float _LineAlpha;
			float _Intensity;

			float _DistanceFade;
			float _FadeStart;
			float _FadeEnd;

			float _PulseStrength;
			float _PulseScale;
			float _PulseSpeed;

			v2f vert(appdata input)
			{
				v2f output;

				output.position = UnityObjectToClipPos(input.vertex);
				output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
				output.color = input.color * _TintColor;

				return output;
			}

			float4 frag(v2f input) : SV_Target
			{
				float fadeRange = max(_FadeEnd - _FadeStart, 0.0001);
				float distanceToCamera = distance(input.worldPosition.xy, _WorldSpaceCameraPos.xy);
				float fadeT = saturate((distanceToCamera - _FadeStart) / fadeRange);

				float distanceFade = lerp(
					1.0,
					1.0 - fadeT,
					saturate(_DistanceFade)
				);

				float pulse = sin(
					(input.worldPosition.x + input.worldPosition.y) * _PulseScale +
					_Time.y * _PulseSpeed
				) * 0.5 + 0.5;

				float finalIntensity = _Intensity + pulse * _PulseStrength;

				float4 outputColor;
				outputColor.rgb = input.color.rgb * finalIntensity;
				outputColor.a = saturate(input.color.a * _LineAlpha * distanceFade);

				return outputColor;
			}
			ENDCG
		}
	}
}