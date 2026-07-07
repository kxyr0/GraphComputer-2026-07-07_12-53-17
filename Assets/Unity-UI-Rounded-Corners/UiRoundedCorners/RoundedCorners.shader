Shader "UI/RoundedCorners/RoundedCorners"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector] _TextureSampleAdd ("Texture Sample Add", Vector) = (0,0,0,0)

        // --- Mask support ---
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        _WidthHeightRadius ("WidthHeightRadius", Vector) = (0,0,0,0)
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
        ColorMask [_ColorMask]

        // ВАЖНО:
        // Это обычный straight-alpha blend.
        // Поэтому RGB нельзя умножать на rounded alpha.
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #include "UnityCG.cginc"
            #include "SDFUtils.cginc"
            #include "ShaderSetup.cginc"

            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            fixed4 _TextureSampleAdd;

            float4 _WidthHeightRadius;
            float _UseUIAlphaClip;

            fixed4 frag(v2f i) : SV_Target
            {
                float roundedAlpha = CalcAlpha(
                    i.uv,
                    _WidthHeightRadius.xy,
                    _WidthHeightRadius.z
                );

                roundedAlpha = saturate(roundedAlpha);

                fixed4 color = tex2D(_MainTex, i.uv) + _TextureSampleAdd;

                // Нормальное поведение Unity UI:
                // texture color * Image color / vertex color.
                color *= i.color;

                // Главное исправление:
                // скругление влияет ТОЛЬКО на alpha.
                color.a *= roundedAlpha;

                if (_UseUIAlphaClip > 0.5)
                {
                    clip(color.a - 0.001);
                }

                return color;
            }

            ENDCG
        }
    }
}