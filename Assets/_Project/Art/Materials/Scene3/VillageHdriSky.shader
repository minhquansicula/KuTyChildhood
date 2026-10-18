Shader "KuTy/HDRI/Sky With Distant Horizon"
{
    Properties
    {
        [NoScaleOffset] _MainTex("HDR panorama",2D)="grey"{}
        _Tint("Warm tint",Color)=(.6,.53,.44,1)
        _Exposure("Exposure",Float)=1
        _Rotation("Panorama rotation",Float)=0
        _LandscapeScale("Background landscape reduction",Range(1,8))=5
        _ClearSkyAngle("Clear upper panorama, degrees",Range(50,85))=80
        _HorizonColor("Warm horizon haze",Color)=(.90,.60,.34,1)
        _SunDirection("Sun direction",Vector)=(-.32,.11,.94,0)
        _SourceSunUV("Photographed sun UV",Vector)=(.5,.55,0,0)
        [HDR] _SunColor("Sun radiance",Color)=(3,2,.9,1)
    }
    SubShader
    {
        Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"}
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            #include "VillageAtmosphere.hlsl"
            sampler2D _MainTex;half4 _MainTex_HDR,_Tint,_SunColor,_HorizonColor;float4 _SunDirection,_SourceSunUV;
            float _Exposure,_Rotation,_LandscapeScale,_ClearSkyAngle;
            struct A {float4 vertex:POSITION;};
            struct V {float4 position:SV_POSITION;float3 direction:TEXCOORD0;};
            V Vert(A i) {V o;o.position=UnityObjectToClipPos(i.vertex);o.direction=i.vertex.xyz;return o;}
            half4 Frag(V i):SV_Target
            {
                float3 world=normalize(i.direction);float sine,cosine;sincos(_Rotation*UNITY_PI/180,sine,cosine);
                float2 azimuth=mul(float2x2(cosine,-sine,sine,cosine),world.xz);
                float elevation=asin(clamp(world.y,-1,1));
                float clear=_ClearSkyAngle*UNITY_PI/180,band=clear/_LandscapeScale;
                // Keep the full photographed landscape. Reduce its angular width and height
                // into a distant scenery ring instead of discarding the lower panorama.
                float source=elevation<band?elevation*_LandscapeScale:lerp(clear,UNITY_PI*.5,saturate((elevation-band)/(UNITY_PI*.5-band)));
                float longitude=.5-atan2(azimuth.y,azimuth.x)/(UNITY_PI*2);
                // Let the repeating sampler wrap U; frac here breaks mip derivatives at every join.
                float2 uv=float2(longitude*_LandscapeScale,.5+source/UNITY_PI);
                // atan2 still jumps by a whole revolution at its branch cut. Unwrap
                // screen-space gradients before mip selection, otherwise a thin strip
                // samples the panorama's bright, averaged smallest mip like a beacon.
                float2 uvDx=ddx(uv),uvDy=ddy(uv);
                uvDx.x-=_LandscapeScale*round(uvDx.x/_LandscapeScale);
                uvDy.x-=_LandscapeScale*round(uvDy.x/_LandscapeScale);
                half3 panorama=DecodeHDR(tex2Dgrad(_MainTex,uv,uvDx,uvDy),_MainTex_HDR);
                // Landscape repetition must not repeat the photographed sun disc.
                float sunDX=frac(uv.x-_SourceSunUV.x+.5)-.5;
                float sunDistance=length(float2(sunDX*2,uv.y-_SourceSunUV.y));
                float eraseSun=1-smoothstep(.01,.035,sunDistance);
                if(eraseSun>.001)
                {
                    half3 neighbour=DecodeHDR(tex2Dgrad(_MainTex,uv+float2(.04,0),uvDx,uvDy),_MainTex_HDR);
                    panorama=lerp(panorama,neighbour,eraseSun);
                }
                half3 color=panorama*_Tint.rgb*unity_ColorSpaceDouble.rgb*_Exposure;
                half haze=1-smoothstep(0,.13,saturate(world.y));
                color=lerp(color,_HorizonColor.rgb,haze*.12);
                float alignment=dot(world,normalize(_SunDirection.xyz));
                float disk=smoothstep(cos(.60*UNITY_PI/180),cos(.45*UNITY_PI/180),alignment);
                float halo=pow(saturate(alignment),700)*.14;
                color+=_SunColor.rgb*(disk+halo);
                color=VillageHeightFog(color,_WorldSpaceCameraPos+world*350);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
