Shader "KuTy/Village/Imported Bambusa"
{
    Properties
    {
        [NoScaleOffset] _Front("Front diffuse and opacity",2DArray)=""{}
        [NoScaleOffset] _Back("Back diffuse and opacity",2DArray)=""{}
        [NoScaleOffset] _Normals("Source normals",2DArray)=""{}
        _Tint("Tint",Color)=(1,1,1,1)
        _Cutoff("Leaf alpha cutoff",Range(0,1))=.4
        _NormalStrength("Normal strength",Range(0,1))=.3
        _WindAmplitude("Leaf wind metres",Float)=.025
        [HideInInspector] _BambooLodRange("LOD coverage interval",Vector)=(0,1,0,0)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
        Cull Off ZWrite On
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "VillageAtmosphere.hlsl"
        CBUFFER_START(UnityPerMaterial)
        half4 _Tint;float _Cutoff,_WindAmplitude,_NormalStrength;
        CBUFFER_END
        UNITY_INSTANCING_BUFFER_START(BambusaInstances)
            UNITY_DEFINE_INSTANCED_PROP(float4,_BambooLodRange)
        UNITY_INSTANCING_BUFFER_END(BambusaInstances)
        void ClipBambusaLod(float2 pixel)
        {
            float2 interval=UNITY_ACCESS_INSTANCED_PROP(BambusaInstances,_BambooLodRange).xy;
            float noise=frac(52.9829189*frac(dot(floor(pixel),float2(.06711056,.00583715))));
            clip(noise-interval.x);clip(interval.y-noise-0.000001);
        }
        TEXTURE2D_ARRAY(_Front);SAMPLER(sampler_Front);
        TEXTURE2D_ARRAY(_Back);SAMPLER(sampler_Back);
        TEXTURE2D_ARRAY(_Normals);SAMPLER(sampler_Normals);
        struct A {float4 p:POSITION;float3 n:NORMAL;float4 tangent:TANGENT;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
        float3 BambusaWorld(A input)
        {
            float3 p=TransformObjectToWorld(input.p.xyz);
            float phase=dot(p.xz,float2(.7,.39))+_Time.y*1.2;
            p.xz+=float2(.8,.6)*(sin(phase)+.2*sin(phase*2.3))*_WindAmplitude*input.color.r;
            return p;
        }
        ENDHLSL
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            struct V {float4 p:SV_POSITION;float3 world:TEXCOORD0;half3 n:TEXCOORD1;half3 t:TEXCOORD2;half3 b:TEXCOORD3;float2 uv:TEXCOORD4;half2 masks:TEXCOORD5;float fog:TEXCOORD6;UNITY_VERTEX_INPUT_INSTANCE_ID};
            V Vert(A input)
            {
                UNITY_SETUP_INSTANCE_ID(input);V o;
                UNITY_TRANSFER_INSTANCE_ID(input,o);
                o.world=BambusaWorld(input);o.p=TransformWorldToHClip(o.world);
                VertexNormalInputs basis=GetVertexNormalInputs(input.n,input.tangent);
                o.n=basis.normalWS;o.t=basis.tangentWS;o.b=basis.bitangentWS;
                o.uv=input.uv;o.masks=input.color.gb;o.fog=ComputeFogFactor(o.p.z);return o;
            }
            half4 Frag(V input,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);ClipBambusaLod(input.p.xy);
                float layer=round(input.masks.y);
                half4 front=SAMPLE_TEXTURE2D_ARRAY(_Front,sampler_Front,input.uv,layer);
                half4 back=SAMPLE_TEXTURE2D_ARRAY(_Back,sampler_Back,input.uv,layer);
                half4 texel=IS_FRONT_VFACE(face,front,back);clip(texel.a-_Cutoff);
                half3 local=SAMPLE_TEXTURE2D_ARRAY(_Normals,sampler_Normals,input.uv,layer).xyz*2-1;
                local.xy*=_NormalStrength;local=normalize(local);
                half3 n=normalize(input.t*local.x+input.b*local.y+input.n*local.z)*IS_FRONT_VFACE(face,1,-1);
                Light sun=GetMainLight(TransformWorldToShadowCoord(input.world));
                half leaf=input.masks.x;
                half diffuse=saturate(dot(n,sun.direction)*.65+.35);
                half transmission=pow(saturate(dot(-n,sun.direction)),2)*leaf*.25;
                half3 ambient=max(SampleSH(n),half3(.22,.24,.20));
                half3 color=texel.rgb*_Tint.rgb*(ambient+sun.color*(diffuse+transmission)*lerp(.35,1,sun.shadowAttenuation));
                return half4(lerp(MixFog(color,input.fog),VillageHeightFog(color,input.world),_VillageFogParameters.w),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex VertDepth
            #pragma fragment FragDepth
            #pragma multi_compile_instancing
            struct D {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float layer:TEXCOORD1;UNITY_VERTEX_INPUT_INSTANCE_ID};
            D VertDepth(A input){UNITY_SETUP_INSTANCE_ID(input);D o;UNITY_TRANSFER_INSTANCE_ID(input,o);o.p=TransformWorldToHClip(BambusaWorld(input));o.uv=input.uv;o.layer=input.color.b;return o;}
            half4 FragDepth(D input):SV_Target {UNITY_SETUP_INSTANCE_ID(input);ClipBambusaLod(input.p.xy);clip(SAMPLE_TEXTURE2D_ARRAY(_Front,sampler_Front,input.uv,round(input.layer)).a-_Cutoff);return 0;}
            ENDHLSL
        }
    }
}
