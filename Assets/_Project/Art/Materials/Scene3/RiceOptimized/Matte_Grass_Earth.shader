Shader "KuTy/Village/Matte Grass Earth"
{
    Properties
    {
        _SoilMap("Earth texture",2D)="white"{}
        _GrassColor("Grass colour",Color)=(.24,.32,.09,1)
        _SoilColor("Earth tint",Color)=(.65,.58,.4,1)
        _GrassCoverage("Grass coverage",Range(0,1))=1
        _UseVertexCoverage("Use bank grass mask",Float)=0
        _SoilScale("Earth scale in world metres",Float)=.7
        _GrassBend("Grass blade wind",Range(0,.1))=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Cull Off ZWrite On
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_SoilMap); SAMPLER(sampler_SoilMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _GrassColor,_SoilColor;
            float _GrassCoverage,_UseVertexCoverage,_SoilScale,_GrassBend;
            CBUFFER_END
            struct A {float4 p:POSITION;float3 n:NORMAL;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V {float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float mask:TEXCOORD2;float fog:TEXCOORD3;float2 blade:TEXCOORD4;};
            float Hash(float2 p) {return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float Noise(float2 p)
            {
                float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
            }
            V Vert(A input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                V o;o.world=TransformObjectToWorld(input.p.xyz);
                o.world.xz+=float2(.8,.6)*sin(_Time.y*1.6+dot(o.world.xz,float2(.9,.6)))*_GrassBend*input.color.g*input.color.g;
                o.p=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(input.n);
                o.mask=lerp(1,input.color.r,_UseVertexCoverage);o.fog=ComputeFogFactor(o.p.z);
                o.blade=input.color.gb;return o;
            }
            half4 Frag(V input,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                float2 p=input.world.xz;
                float macro=Noise(p*.42),patch=Noise(p*3.5);
                float attenuation=1-smoothstep(.4,1.5,max(fwidth(p.x*22),fwidth(p.y*22)));
                float grain=lerp(.5,Noise(p*22),attenuation);
                half3 grass=_GrassColor.rgb*(.7+macro*.45+patch*.18+grain*.15);
                grass=lerp(grass,grass*half3(1.1,1.02,.68),saturate((patch-.6)*1.3));
                // Thin blades use a soft upward normal, with colour variation per blade.
                // This avoids dark, rigid-looking strips without adding glossy lighting.
                float bladeMaterial=step(.001,_GrassBend);
                grass*=lerp(1,lerp(.78,1.25,input.blade.y)*(1+input.blade.x*.08),bladeMaterial);
                half3 soil=SAMPLE_TEXTURE2D(_SoilMap,sampler_SoilMap,p*_SoilScale).rgb*_SoilColor.rgb;
                float mask=saturate(input.mask*_GrassCoverage-(patch-.5)*.3);
                half3 albedo=lerp(soil,grass,mask);
                half3 n=normalize(input.normal)*IS_FRONT_VFACE(face,1,-1);
                n=normalize(lerp(n,half3(0,1,0),bladeMaterial*.65));
                Light sun=GetMainLight(TransformWorldToShadowCoord(input.world));
                half diffuse=saturate((dot(n,sun.direction)+.15)/1.15);
                half3 light=SampleSH(n)+sun.color*diffuse*sun.shadowAttenuation;
                return half4(MixFog(albedo*max(light,.2),input.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags {"LightMode"="DepthOnly"}
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _GrassColor,_SoilColor;
            float _GrassCoverage,_UseVertexCoverage,_SoilScale,_GrassBend;
            CBUFFER_END
            struct A {float4 p:POSITION;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            float4 DepthVert(A input):SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 world=TransformObjectToWorld(input.p.xyz);
                world.xz+=float2(.8,.6)*sin(_Time.y*1.6+dot(world.xz,float2(.9,.6)))*_GrassBend*input.color.g*input.color.g;
                return TransformWorldToHClip(world);
            }
            half4 DepthFrag():SV_Target {return 0;}
            ENDHLSL
        }
    }
}
