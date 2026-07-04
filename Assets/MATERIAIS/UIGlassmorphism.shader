Shader "UI/Glassmorphism"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlassColor ("Cor do Vidro", Color) = (0.8, 0.9, 1.0, 0.25)
        _BorderColor ("Cor da Borda", Color) = (1, 1, 1, 0.8)
        _BorderWidth ("Largura da Borda", Range(0, 0.1)) = 0.02
        _BorderSmooth ("Suavidade da Borda", Range(0.001, 0.05)) = 0.005
        _CornerRadius ("Raio dos Cantos", Range(0, 0.5)) = 0.1
        _Shine ("Brilho Diagonal", Range(0, 1)) = 0.15
        _GlobalAlpha ("Alpha Global", Range(0, 1)) = 1.0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _GlassColor;
            fixed4 _BorderColor;
            float _BorderWidth;
            float _BorderSmooth;
            float _CornerRadius;
            float _Shine;
            float _GlobalAlpha;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            float roundedBoxSDF(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float2 centered = uv - 0.5;

                float dist = roundedBoxSDF(centered, float2(0.5, 0.5), _CornerRadius);
                float mask = 1.0 - smoothstep(0.0, _BorderSmooth, dist);

                float bordaDentro = -dist;
                float borda = smoothstep(_BorderWidth + _BorderSmooth, _BorderWidth, bordaDentro);

                fixed4 corVidro = _GlassColor;

                float sheen = smoothstep(0.2, 0.8, uv.x * 0.5 + (1.0 - uv.y) * 0.5);
                corVidro.rgb += sheen * _Shine;

                fixed4 corFinal = lerp(corVidro, _BorderColor, borda);

                // Aplica máscara dos cantos + GlobalAlpha
                corFinal.a *= mask * _GlobalAlpha;

                corFinal *= IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                corFinal.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(corFinal.a - 0.001);
                #endif

                return corFinal;
            }
            ENDCG
        }
    }
}