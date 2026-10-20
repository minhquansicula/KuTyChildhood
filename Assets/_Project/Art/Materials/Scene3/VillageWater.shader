Shader "KuTy/VillageWater"
{
    Properties
    {
        _ShallowColor("Shallow water",Color)=(.16,.36,.42,1)
        _DeepColor("Deep water",Color)=(.045,.16,.23,1)
        _HorizonColor("Sky horizon",Color)=(.4,.52,.6,1)
        _ZenithColor("Sky zenith",Color)=(.12,.3,.48,1)
        _Opacity("Water opacity",Range(0,1))=.5
        _WaveStrength("Wave normal strength",Range(0,.15))=.032
        _FlowSpeed("Flow speed",Float)=.8
        _DepthTone("Deep tint",Range(0,1))=.65
        _ReflectionRoughness("Reflection roughness",Range(0,1))=.15
        _EnvironmentIntensity("HDRI intensity",Range(0,1))=.8
        [HideInInspector] _SurfaceIndex("Surface index",Float)=0
        [HideInInspector] _SurfaceMeander("Curved surface",Float)=0
        [HideInInspector] _UseShoreMask("Mesh shoreline distance",Float)=0
        [HideInInspector] _SurfaceBounds("Surface bounds",Vector)=(-100,-100,100,100)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Name "WaterForward"
            Tags {"LightMode"="UniversalForward"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "VillageAtmosphere.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor,_DeepColor,_HorizonColor,_ZenithColor;
                float4 _SurfaceBounds;
                float _Opacity,_WaveStrength,_FlowSpeed,_DepthTone,_SurfaceIndex,_SurfaceMeander,_UseShoreMask,_ReflectionRoughness,_EnvironmentIntensity;
            CBUFFER_END
            float4 _VillageWaterRipples[8];
            float4 _VillageWaterRippleParams[8];
            int _VillageWaterRippleCount;
            struct Attributes {float4 positionOS:POSITION;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half fog:TEXCOORD1;float shore:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO};
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(output.positionWS);
                output.fog=ComputeFogFactor(output.positionCS.z);
                output.shore=input.color.r;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 p=input.positionWS.xz;
                float time=_Time.y*_FlowSpeed;
                float phase1=dot(p,float2(.86,.32))*3.1-time*.7;
                float phase2=dot(p,float2(-.45,.89))*5.2-time*1.05;
                // Filter fine normal detail at distance to avoid the old shimmering.
                float filter1=1-smoothstep(.6,2.2,fwidth(phase1));
                float filter2=1-smoothstep(.6,2.2,fwidth(phase2));
                float2 slope=_WaveStrength*(float2(.86,.32)*cos(phase1)*filter1+float2(-.45,.89)*cos(phase2)*filter2*.65);
                float foam=0;
                [loop] for(int i=0;i<_VillageWaterRippleCount;i++)
                {
                    float4 data=_VillageWaterRippleParams[i];
                    float age=_Time.y-_VillageWaterRipples[i].w;
                    if(abs(data.y-_SurfaceIndex)<.25 && age>=0 && age<data.z)
                    {
                        float2 delta=p-_VillageWaterRipples[i].xz;
                        float distance=max(length(delta),.001);
                        float ring=distance-age*data.w;
                        float band=exp(-abs(ring)*12)*(1-age/data.z)*data.x;
                        slope+=delta/distance*cos(ring*17)*band*.14;
                        foam+=band*.28;
                    }
                }
                float3 normal=normalize(float3(-slope.x,1,-slope.y));
                float3 view=GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 reflected=reflect(-view,normal);
                half fresnel=.07+.85*pow(1-saturate(dot(normal,view)),3);
                half3 sky=lerp(_HorizonColor.rgb,_ZenithColor.rgb,saturate(reflected.y));
                half3 environment=GlossyEnvironmentReflection(reflected,_ReflectionRoughness,1.0h)*_EnvironmentIntensity;
                half3 reflection=lerp(sky,environment,.55h);
                half3 water=lerp(_ShallowColor.rgb,_DeepColor.rgb,_DepthTone);
                Light sun=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half glint=pow(saturate(dot(normal,normalize(view+sun.direction))),72)*.85h;
                half3 color=lerp(water*(.45+.55*sun.shadowAttenuation),reflection,fresnel)+min(sun.color,half3(1.2,1.2,1.2))*glint*sun.shadowAttenuation;
                half sheen=saturate(cos(phase1)*filter1*.6+cos(phase2)*filter2*.4-.3)*_WaveStrength;
                color+=half3(.4,.65,.8)*sheen;
                float bendT=saturate((p.y+5)/13);
                float2 bankPoint=p;
                bankPoint.x-=2.6*sin((p.y-18.5)*3.14159265/25.5)*bendT*bendT*(3-2*bendT)*_SurfaceMeander;
                float edge=min(min(bankPoint.x-_SurfaceBounds.x,_SurfaceBounds.z-bankPoint.x),min(bankPoint.y-_SurfaceBounds.y,_SurfaceBounds.w-bankPoint.y));
                edge=lerp(edge,input.shore,_UseShoreMask);
                foam+=(1-smoothstep(.008,.065,edge))*(.035+.02*sin(phase1));
                color=lerp(color,half3(.7,.86,.9),saturate(foam));
                color=lerp(MixFog(color,input.fog),VillageHeightFog(color,input.positionWS),_VillageFogParameters.w);
                return half4(color,saturate(_Opacity+fresnel*.4+foam*.18));
            }
            ENDHLSL
        }
    }
}
