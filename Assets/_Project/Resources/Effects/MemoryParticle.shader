Shader "KuTy/Memory Particle"
{
    Properties
    {
        [MainTexture] _BaseMap("Soft particle mask", 2D) = "white" {}
        _Brightness("Brightness", Float) = 1
        _SoftFade("Depth intersection fade", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _Brightness;
                float _SoftFade;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float eyeDepth : TEXCOORD2;
                half fog : TEXCOORD3;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertex = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertex.positionCS;
                output.screenPos = ComputeScreenPos(vertex.positionCS);
                output.eyeDepth = -vertex.positionVS.z;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                output.fog = ComputeFogFactor(vertex.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 mask = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 color = half4(input.color.rgb * mask.rgb * _Brightness, input.color.a * mask.a);
                if (_SoftFade > .001)
                {
                    float depth = SampleSceneDepth(input.screenPos.xy / input.screenPos.w);
                    float sceneEye = LinearEyeDepth(depth, _ZBufferParams);
                    color.a *= saturate((sceneEye - input.eyeDepth) / _SoftFade);
                }
                color.a *= smoothstep(.04, .22, input.eyeDepth);
                color.rgb = MixFog(color.rgb, input.fog);
                return color;
            }
            ENDHLSL
        }
    }
}
