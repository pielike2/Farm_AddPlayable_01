Shader "Custom/Diffuse ShadowsOnly Cutout" {
Properties {
    _MainTex ("Base (RGB)", 2D) = "white" {}
    _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
}
SubShader {
    Tags { 
        "RenderType"="TransparentCutout" 
        "Queue"="AlphaTest"
    }
    LOD 200

    // Main pass - completely transparent (invisible)
    Pass {
        Tags { "LightMode"="ForwardBase" }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ColorMask 0  // Don't write any color
        
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        
        struct appdata {
            float4 vertex : POSITION;
        };
        
        struct v2f {
            float4 vertex : SV_POSITION;
        };
        
        v2f vert (appdata v) {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            return o;
        }
        
        fixed4 frag (v2f i) : SV_Target {
            return fixed4(0,0,0,0); // Completely transparent
        }
        ENDCG
    }

    // Shadow caster pass with blue channel cutout
    Pass {
        Tags { "LightMode"="ShadowCaster" }
        
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma multi_compile_shadowcaster
        #include "UnityCG.cginc"
        
        sampler2D _MainTex;
        float4 _MainTex_ST;
        fixed _Cutoff;
        
        struct appdata {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float2 texcoord : TEXCOORD0;
        };
        
        struct v2f {
            V2F_SHADOW_CASTER;
            float2 uv : TEXCOORD1;
        };
        
        v2f vert(appdata v) {
            v2f o;
            o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
            TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
            return o;
        }
        
        float4 frag(v2f i) : SV_Target {
            fixed4 texcol = tex2D(_MainTex, i.uv);
            clip(texcol.rgb - _Cutoff); // TODO cutout mask
            SHADOW_CASTER_FRAGMENT(i)
        }
        ENDCG
    }
}

Fallback "Legacy Shaders/Transparent/Cutout/VertexLit"
}