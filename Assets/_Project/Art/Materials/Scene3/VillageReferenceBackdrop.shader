Shader "KuTy/Village/Golden Afternoon Backdrop"
{
    Properties
    {
        [NoScaleOffset] _MainTex("Full 360 panorama", 2D) = "grey" {}
        _Exposure("Exposure", Range(.25,2)) = 1
        _Rotation("Longitude rotation", Float) = 0
        _HorizonOffset("Horizon framing", Range(-.1,.1)) = -.045
        _HorizonColor("Horizon haze", Color) = (.88,.81,.66,1)
        _SunDirection("Sun direction", Vector) = (.63,.53,-.57,0)
        [HDR] _SunColor("Sun radiance", Color) = (4,3,1.5,1)
        _SunRadius("Sun radius in degrees", Range(.1,2)) = .8
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Exposure, _Rotation, _HorizonOffset, _SunRadius;
            float4 _SunDirection;
            half4 _SunColor, _HorizonColor;
            struct A { float4 vertex : POSITION; };
            struct V { float4 position : SV_POSITION; float3 direction : TEXCOORD0; };
            V Vert(A input)
            {
                V output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.direction = input.vertex.xyz;
                return output;
            }
            half4 Frag(V input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                float u = .5 - atan2(direction.z, direction.x) / (2 * UNITY_PI) - _Rotation / 360;
                float v = .5 + asin(clamp(direction.y,-1,1)) / UNITY_PI + _HorizonOffset;
                float2 uv = float2(u, saturate(v));
                float2 dx = ddx(uv), dy = ddy(uv);
                // Correct atan2's branch cut before selecting a mip. Longitude spans exactly
                // one panorama, so mountains, cloud banks and solar highlights never repeat.
                dx.x -= round(dx.x); dy.x -= round(dy.x);
                half3 color = tex2Dgrad(_MainTex, uv, dx, dy).rgb;
                // Blend a narrow strip at the longitude join for a continuous generated panorama.
                // Do not multiply longitude or mirror the scene around the player.
                float edge = frac(u);
                float join = 1 - smoothstep(0,.012,min(edge,1-edge));
                float2 otherUV = float2(1-edge, uv.y);
                half3 oppositeEdge = tex2Dgrad(_MainTex,otherUV,dx,dy).rgb;
                color = lerp(color,(color+oppositeEdge)*.5,join);
                // The horizon offset must not make the zenith sample a different blue for
                // each longitude. Converge to one pole colour to remove the pinwheel seam.
                half3 zenith = tex2Dlod(_MainTex,float4(.5,.995,0,0)).rgb;
                color = lerp(color,zenith,smoothstep(.92,.995,direction.y));
                color *= _Exposure;
                float haze = (1-smoothstep(0,.055,abs(direction.y))) * .1;
                color = lerp(color,_HorizonColor.rgb,haze);
                float alignment = dot(direction,normalize(_SunDirection.xyz));
                float radius = _SunRadius * UNITY_PI / 180;
                float disk = smoothstep(cos(radius*1.08),cos(radius*.92),alignment);
                float halo = pow(saturate(alignment),180) * .08;
                // The source image contains no sun disc. Add precisely one, aligned to the
                // existing directional light, instead of inheriting photographed duplicate suns.
                color += _SunColor.rgb * (disk+halo);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
