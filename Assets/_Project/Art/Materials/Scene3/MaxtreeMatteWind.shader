Shader "KuTy/Village/Maxtree Matte Wind"
{
    Properties
    {
        [MainTexture] _BaseMap("Packed albedo and opacity",2D)="white"{}
        _HasTexture("Use original Maxtree maps",Float)=0
        _Cutoff("Leaf alpha cutoff",Range(0,1))=.35
        _LeafColor("Leaf colour (fallback without Maps)",Color)=(.27,.34,.14,1)
        _BarkColor("Bark colour (fallback without Maps)",Color)=(.26,.20,.14,1)
        _WindAmplitude("Wind metres",Float)=.035
        _Flexibility("Player bending",Range(0,1))=1
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Cull Off ZWrite On
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "VillageAtmosphere.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST,_LeafColor,_BarkColor;float _WindAmplitude,_Flexibility,_HasTexture,_Cutoff;
        CBUFFER_END
        TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
        float4 _VillageRiceInteractor;
        struct A {float4 p:POSITION;float3 n:NORMAL;float4 color:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
        float3 PlantPosition(A input)
        {
            float3 p=TransformObjectToWorld(input.p.xyz);float mask=input.color.r*input.color.r;
            float phase=dot(p.xz,float2(.7,.5))+_Time.y*1.5;
            float wind=sin(phase)+.3*sin(phase*2.8+_Time.y*.6);
            p.xz+=float2(.8,.6)*wind*_WindAmplitude*mask;
            float2 delta=p.xz-_VillageRiceInteractor.xz;float d=length(delta);
            float bend=saturate(1-d/max(_VillageRiceInteractor.w,.01))*step(.01,_VillageRiceInteractor.w)*_Flexibility;
            p.xz+=delta/max(.05,d)*bend*.12*mask;return p;
        }
        ENDHLSL
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            struct V {float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float4 color:TEXCOORD2;float fog:TEXCOORD3;float variation:TEXCOORD4;float2 uv:TEXCOORD5;};
            V Vert(A input)
            {
                UNITY_SETUP_INSTANCE_ID(input);V o;o.world=PlantPosition(input);o.p=TransformWorldToHClip(o.world);
                o.normal=TransformObjectToWorldNormal(input.n);o.color=input.color;o.fog=ComputeFogFactor(o.p.z);
                o.uv=TRANSFORM_TEX(input.uv,_BaseMap);
                float3 origin=TransformObjectToWorld(float3(0,0,0));o.variation=.86+.22*frac(sin(dot(origin.xz,float2(12.99,78.23)))*43758.54);return o;
            }
            half4 Frag(V input,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                float leaf=step(.65,input.color.g);
                half3 albedo=lerp(_BarkColor.rgb,_LeafColor.rgb,leaf);
                half4 texel=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv);
                clip(lerp(1,texel.a,_HasTexture)-_Cutoff);
                albedo=lerp(albedo,texel.rgb,_HasTexture);
                albedo*=lerp(.7,1.08,input.color.r)*input.variation;
                half3 n=normalize(input.normal)*IS_FRONT_VFACE(face,1,-1);
                n=normalize(lerp(n,half3(0,1,0),leaf*.35));
                Light sun=GetMainLight(TransformWorldToShadowCoord(input.world));
                half diffuse=saturate((dot(n,sun.direction)+.3)/1.3);
                half3 light=SampleSH(n)+sun.color*diffuse*sun.shadowAttenuation;
                half3 color=albedo*max(light,.19);
                return half4(lerp(MixFog(color,input.fog),VillageHeightFog(color,input.world),_VillageFogParameters.w),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection,_LightPosition;
            struct S {float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
            S ShadowVert(A input)
            {
                UNITY_SETUP_INSTANCE_ID(input);S o;float3 p=PlantPosition(input);float3 n=TransformObjectToWorldNormal(input.n);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirection=normalize(_LightPosition-p);
                #else
                    float3 lightDirection=_LightDirection;
                #endif
                o.p=TransformWorldToHClip(ApplyShadowBias(p,n,lightDirection));
                #if UNITY_REVERSED_Z
                    o.p.z=min(o.p.z,UNITY_NEAR_CLIP_VALUE*o.p.w);
                #else
                    o.p.z=max(o.p.z,UNITY_NEAR_CLIP_VALUE*o.p.w);
                #endif
                o.uv=TRANSFORM_TEX(input.uv,_BaseMap);return o;
            }
            half4 ShadowFrag(S input):SV_Target {clip(lerp(1,SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).a,_HasTexture)-_Cutoff);return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ColorMask 0
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            struct D {float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
            D DepthVert(A input) {UNITY_SETUP_INSTANCE_ID(input);D o;o.p=TransformWorldToHClip(PlantPosition(input));o.uv=TRANSFORM_TEX(input.uv,_BaseMap);return o;}
            half4 DepthFrag(D input):SV_Target {clip(lerp(1,SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).a,_HasTexture)-_Cutoff);return 0;}
            ENDHLSL
        }
    }
}
