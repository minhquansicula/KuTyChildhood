Shader "KuTy/Household Water"
{
    Properties
    {
        _BaseColor("Water tint", Color) = (0.22,0.35,0.32,0.22)
        _Smoothness("Smoothness", Range(0,1)) = 0.94
        _WaveStrength("Small wave slope", Range(0,0.1)) = 0.018
        _Ripple("Ripple center XZ, time, strength", Vector) = (0,0,-100,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "WaterForward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Smoothness, _WaveStrength;
                float4 _Ripple;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; half fog:TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.fog = ComputeFogFactor(p.positionCS.z); return o;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 p = input.positionWS.xz;
                float t = _Time.y;
                float2 slope = float2(cos(dot(p,float2(19,11))+t*.8), sin(dot(p,float2(-13,23))-t*.65)) * _WaveStrength;
                float age = max(0, t - _Ripple.z);
                float2 delta = p - _Ripple.xy; float radius = length(delta);
                float wave = cos(radius*95-age*9) * exp(-radius*5-age*1.8) * _Ripple.w;
                slope += delta / max(radius,.005) * wave * .065;
                half3 n = normalize(input.normalWS + half3(slope.x,0,slope.y) * saturate(input.normalWS.y));
                half3 v = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half nv = saturate(dot(n,v));
                half fresnel = .02h + .98h * pow(1-nv,5);
                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 h = SafeNormalize(v + light.direction);
                half spec = pow(saturate(dot(n,h)), lerp(64,512,_Smoothness)) * 2.5h;
                half3 reflection = GlossyEnvironmentReflection(reflect(-v,n), input.positionWS, 1-_Smoothness, 1);
                half3 ambient = SampleSH(n);
                half3 tint = _BaseColor.rgb * (ambient + light.color * saturate(dot(n,light.direction)) * .18h);
                half3 color = lerp(tint, reflection, saturate(.20h+fresnel*.8h));
                color += light.color * spec * light.shadowAttenuation * light.distanceAttenuation;
                half alpha = saturate(_BaseColor.a + fresnel*.55h + spec*.12h);
                return half4(MixFog(color,input.fog),alpha);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Lit"
}
