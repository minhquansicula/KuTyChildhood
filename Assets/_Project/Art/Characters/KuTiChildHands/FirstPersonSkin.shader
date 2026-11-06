Shader "KuTy/FirstPersonSkin"
{
    Properties
    {
        _BaseColor("Skin or cloth colour", Color) = (0.72, 0.53, 0.39, 1)
        _Fill("Soft ambient fill", Range(0, 1)) = 0.38
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "FirstPersonForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Fill;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half3 normal = normalize(input.normalWS);
                Light sun = GetMainLight();
                half diffuse = saturate(dot(normal, sun.direction));
                // Close-up hands keep scene light direction without screen-space AO blotches.
                half3 ambient = max(SampleSH(normal), half3(_Fill, _Fill, _Fill));
                half maximum = max(1.0h, max(sun.color.r, max(sun.color.g, sun.color.b)));
                half3 lighting = ambient + (sun.color / maximum) * (0.12h + 0.55h * diffuse);
                return half4(_BaseColor.rgb * lighting, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
