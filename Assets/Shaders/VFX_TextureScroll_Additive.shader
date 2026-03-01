Shader "Custom/VFX/TextureScroll Additive"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _SecondaryTex ("Secondary Texture (static)", 2D) = "white" {}
        _Tint("Tint Color", Color) = (1,1,1,1)
        _ScrollSpeed("Scroll Speed (X, Y)", Vector) = (1, 0, 0, 0)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        LOD 100

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _SecondaryTex;
            float4 _SecondaryTex_ST;
            float4 _Tint;
            float2 _ScrollSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uvMain : TEXCOORD0;
                float2 uvMask : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                float2 baseUV = v.uv;

                float t = _Time.y;
                float2 uvOffset = baseUV + _ScrollSpeed * t;
                o.uvMain = TRANSFORM_TEX(uvOffset, _MainTex);

                o.uvMask = TRANSFORM_TEX(baseUV, _SecondaryTex);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uvMain) * _Tint;
                fixed4 secondary = tex2D(_SecondaryTex, i.uvMask);
                return col * secondary;
            }
            ENDCG
        }
    }
}