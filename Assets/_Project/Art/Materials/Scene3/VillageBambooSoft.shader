Shader "KuTy/Village/Soft Bamboo"
{
    Properties
    {
        _LeafA("Olive leaves",Color)=(.20,.34,.09,1)
        _LeafB("Young leaves",Color)=(.34,.46,.13,1)
        _Bark("Culms",Color)=(.31,.36,.15,1)
        _WindAmplitude("Tip wind metres",Float)=.035
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
        half4 _LeafA,_LeafB,_Bark; float _WindAmplitude;
        CBUFFER_END
        struct A {float4 p:POSITION;float3 n:NORMAL;float4 color:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
        float3 BambooWorld(A input)
        {
            float3 p=TransformObjectToWorld(input.p.xyz);
            float phase=dot(p.xz,float2(.61,.39))+_Time.y*1.25;
            p.xz+=float2(.8,.6)*(sin(phase)+.23*sin(phase*2.1))*_WindAmplitude*input.color.r;
            return p;
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
            struct V {float4 p:SV_POSITION;float3 world:TEXCOORD0;half3 n:TEXCOORD1;half4 color:TEXCOORD2;float2 uv:TEXCOORD3;float fog:TEXCOORD4;};
            V Vert(A input)
            {
                UNITY_SETUP_INSTANCE_ID(input);V o;
                o.world=BambooWorld(input);o.p=TransformWorldToHClip(o.world);
                o.n=TransformObjectToWorldNormal(input.n);o.color=input.color;o.uv=input.uv;o.fog=ComputeFogFactor(o.p.z);
                return o;
            }
            half4 Frag(V input,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                half leaf=input.color.g;
                half3 albedo=lerp(_Bark.rgb,lerp(_LeafA.rgb,_LeafB.rgb,input.color.b),leaf);
                albedo*=lerp(.84,1.12,input.color.b);
                half vein=(1-smoothstep(.01,.06,abs(input.uv.x-.5)))*leaf;
                albedo*=1+vein*.13;
                half3 n=normalize(input.n)*IS_FRONT_VFACE(face,1,-1);
                n=normalize(lerp(n,half3(0,1,0),leaf*.3));
                Light sun=GetMainLight(TransformWorldToShadowCoord(input.world));
                half diffuse=saturate(dot(n,sun.direction)*.65+.35);
                half transmission=pow(saturate(dot(-n,sun.direction)),2)*leaf*.22;
                half3 light=max(SampleSH(n),half3(.24,.25,.22))+sun.color*(diffuse+transmission)*lerp(.38,1,sun.shadowAttenuation);
                half3 color=albedo*light;
                return half4(lerp(MixFog(color,input.fog),VillageHeightFog(color,input.world),_VillageFogParameters.w),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ColorMask 0
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            float4 DepthVert(A input):SV_POSITION {UNITY_SETUP_INSTANCE_ID(input);return TransformWorldToHClip(BambooWorld(input));}
            half4 DepthFrag():SV_Target {return 0;}
            ENDHLSL
        }
    }
}
