Shader "KuTy/Soft Earth Path"
{
    Properties { _BaseMap ("Earth texture", 2D) = "white" {} _Tint ("Tint", Color) = (0.67,0.59,0.46,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Input { float4 vertex:POSITION; float4 color:COLOR; };
            struct Output { float4 position:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _Tint;
            Output vert(Input input)
            {
                Output output;
                float3 world = TransformObjectToWorld(input.vertex.xyz);
                output.position = TransformWorldToHClip(world);
                output.uv = world.xz * 0.14;
                output.color = input.color;
                return output;
            }
            float4 frag(Output input):SV_Target
            {
                float4 earth = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv);
                return float4(earth.rgb * _Tint.rgb,input.color.a * _Tint.a);
            }
            ENDHLSL
        }
    }
}
