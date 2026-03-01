Shader "Custom/Diffuse Cutout"
{
    Properties
    {
        _MainTex ("Basic (RGB) Transparent (A)", 2D) = "white" {}
        _Cutoff ("Step cut alpha", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
        LOD 150

        CGPROGRAM
        #pragma surface surf Lambert half alphatest:_Cutoff addshadow
        #pragma target 2.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    
    FallBack "Mobile/Diffuse"
}