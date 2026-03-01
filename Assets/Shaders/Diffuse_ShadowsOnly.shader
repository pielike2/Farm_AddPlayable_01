Shader "Custom/Diffuse ShadowsOnly" {
Properties {
    _MainTex ("Base (RGB)", 2D) = "white" {}
}
SubShader {
    Tags { 
        "RenderType"="Transparent" 
        "Queue"="Transparent"
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

    // Shadow caster pass - this is what creates the shadows
    Pass {
        Tags { "LightMode"="ShadowCaster" }
        
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma multi_compile_shadowcaster
        #include "UnityCG.cginc"
        
        struct v2f {
            V2F_SHADOW_CASTER;
        };
        
        v2f vert(appdata_base v) {
            v2f o;
            TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
            return o;
        }
        
        float4 frag(v2f i) : SV_Target {
            SHADOW_CASTER_FRAGMENT(i)
        }
        ENDCG
    }
}

Fallback "Legacy Shaders/VertexLit"
}