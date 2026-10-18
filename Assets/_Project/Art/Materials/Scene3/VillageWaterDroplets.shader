Shader "KuTy/VillageWaterDroplets"
{
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes {float4 position:POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;};
            struct Varyings {float4 position:SV_POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;};
            Varyings Vert(Attributes input) {Varyings o;o.position=TransformObjectToHClip(input.position.xyz);o.color=input.color;o.uv=input.uv;return o;}
            half4 Frag(Varyings input):SV_Target {half radial=1-smoothstep(.22,.5,length(input.uv-.5));clip(radial-.01);return half4(input.color.rgb,input.color.a*radial);}
            ENDHLSL
        }
    }
}
