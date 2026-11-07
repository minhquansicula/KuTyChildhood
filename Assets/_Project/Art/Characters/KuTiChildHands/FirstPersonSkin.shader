Shader "KuTy/FirstPersonSkin"
{
    Properties
    {
        _BaseColor("Skin or cloth colour", Color) = (0.72, 0.53, 0.39, 1)
        _Fill("Soft ambient fill", Range(0, 1)) = 0.38
        _PalmColor("Palm and fingertip tint", Color) = (0.78, 0.57, 0.53, 1)
        _SkinDetail("Skin detail (zero for cloth)", Range(0, 1)) = 1
        _SkinMap("Source skin detail", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "FirstPersonForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Fill;
                half4 _PalmColor;
                half _SkinDetail;
            CBUFFER_END
            TEXTURE2D(_SkinMap);
            SAMPLER(sampler_SkinMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 anatomy : TEXCOORD1;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half3 skinZones : TEXCOORD3;
                float2 anatomy : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = input.uv;
                output.skinZones = input.color.rgb;
                output.anatomy = input.anatomy;
                return output;
            }
            float Hash(float2 p)
            {
                p = frac(p * float2(.1031, .11369));
                p += dot(p, p.yx + 19.19);
                return frac(p.x * p.y * (p.x + p.y));
            }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x),
                            lerp(Hash(i + float2(0, 1)), Hash(i + 1), f.x), f.y);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half3 normal = normalize(input.normalWS);
                float u = input.anatomy.x, v = input.anatomy.y;
                float line1 = v - (.43 + .12 * (u - .5) - .15 * (u - .5) * (u - .5));
                float line2 = v - (.31 - .10 * (u - .5) + .04 * (u - .5) * (u - .5));
                float life = u - (.59 - .17 * sin(saturate((v - .08) / .48) * PI));
                float across = smoothstep(.10, .22, u) * (1 - smoothstep(.77, .88, u));
                float lifeGate = smoothstep(.08, .14, v) * (1 - smoothstep(.48, .56, v));
                float palmLines = max(
                    max(1 - smoothstep(.0006, .0006 + max(fwidth(line1), .0001), abs(line1)),
                        1 - smoothstep(.0006, .0006 + max(fwidth(line2), .0001), abs(line2))) * across,
                    (1 - smoothstep(.0006, .0006 + max(fwidth(life), .0001), abs(life))) * lifeGate);
                palmLines *= saturate(input.skinZones.b * 2) * _SkinDetail;
                float poreVisibility = 1 - smoothstep(.25, .75,
                    1400 * max(length(ddx(input.uv)), length(ddy(input.uv))));
                float pores = Noise(input.uv * 1400);
                float height = pores * .000008 * poreVisibility * _SkinDetail - palmLines * .000004;
                float3 dx = ddx(input.positionWS), dy = ddy(input.positionWS);
                float3 r1 = cross(dy, normal), r2 = cross(normal, dx);
                float determinant = dot(dx, r1);
                if (abs(determinant) > 1e-12)
                    normal = normalize(normal - (ddx(height) * r1 + ddy(height) * r2) / determinant);
                half warmth = saturate(input.skinZones.r) * _SkinDetail;
                half crease = saturate(input.skinZones.g) * _SkinDetail;
                half3 albedo = lerp(_BaseColor.rgb, _PalmColor.rgb, warmth * .65h);
                half3 source = SAMPLE_TEXTURE2D(_SkinMap, sampler_SkinMap, input.uv).rgb;
                half3 broad = SAMPLE_TEXTURE2D_LOD(_SkinMap, sampler_SkinMap, input.uv, 6).rgb;
                // Preserve fine skin lines, not the generator's large baked shadow patches.
                half residual = dot(source - broad, half3(.2126h, .7152h, .0722h));
                half skinLines = residual / (1 + 18 * abs(residual));
                albedo += skinLines * .40h * _SkinDetail;
                albedo *= 1 - palmLines * .018h;
                albedo *= 1 + _SkinDetail * ((Noise(input.uv * 65) - .5) * .045
                    + (pores - .5) * .016 * poreVisibility - crease * .035);
                Light sun = GetMainLight();
                half diffuse = saturate((dot(normal, sun.direction) + .18h) / 1.18h);
                half3 ambient = max(SampleSH(normal), half3(_Fill, _Fill, _Fill));
                half maximum = max(1.0h, max(sun.color.r, max(sun.color.g, sun.color.b)));
                half3 lightColor = sun.color / maximum;
                half3 lighting = ambient + lightColor * (.12h + .55h * diffuse);
                half3 viewDirection = normalize(GetWorldSpaceViewDir(input.positionWS));
                half3 halfway = normalize(sun.direction + viewDirection);
                half highlight = pow(saturate(dot(normal, halfway)), 18) * .025h * _SkinDetail;
                return half4(albedo * lighting + lightColor * highlight, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
