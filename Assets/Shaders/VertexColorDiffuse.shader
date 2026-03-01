Shader "Custom/VertexColorDiffuse" {
    Properties {
    }

    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 150

        CGPROGRAM
        #pragma surface surf Lambert

        #pragma target 2.0

        struct Input {
            float4 color : COLOR;
        };

        void surf (Input IN, inout SurfaceOutput o) {
            o.Albedo = IN.color.rgb;
            
            o.Alpha = IN.color.a;
        }
        ENDCG
    }
    
    FallBack "Mobile/VertexLit"
}