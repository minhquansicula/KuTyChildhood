Shader "KuTy/Act2/GianBep Aged Walls"
{
    Properties
    {
        [MainTexture] _BaseMap("Original kitchen atlas", 2D) = "white" {}
        [MainColor] _BaseColor("Base tint", Color) = (1,1,1,1)
        [Normal] _BumpMap("Original normal map", 2D) = "bump" {}
        _BumpScale("Normal strength", Float) = 1
        _MetallicGlossMap("Original metallic map", 2D) = "white" {}
        _Metallic("Metallic", Range(0,1)) = 1
        _Smoothness("Original smoothness", Range(0,1)) = 0.2
        _AgedMap("Existing house plaster atlas", 2D) = "white" {}
        _AgedTint("Plaster tint", Color) = (1,1,1,1)
        _AgedStrength("Aged plaster strength", Range(0,1)) = 1
        [HideInInspector] _BoundsMin("Mesh bounds minimum", Vector) = (0,0,0,0)
        [HideInInspector] _BoundsInvSize("Inverse mesh size", Vector) = (1,1,1,0)
        [HideInInspector] _Cutoff("Alpha cutoff", Range(0,1)) = 0.5
        [HideInInspector] _Cull("Cull", Float) = 2
        [HideInInspector] _Surface("Surface", Float) = 0
        [HideInInspector] _EmissionColor("Emission", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            ZWrite On
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex LitPassVertex
            #pragma fragment AgedWallFragment
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"

            TEXTURE2D(_AgedMap);
            SAMPLER(sampler_AgedMap);
            CBUFFER_START(AgedPlasterSettings)
                float4 _AgedMap_ST;
                half4 _AgedTint;
                float4 _BoundsMin;
                float4 _BoundsInvSize;
                half _AgedStrength;
            CBUFFER_END

            half4 AgedWallFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                SurfaceData surface;
                InitializeStandardLitSurfaceData(input.uv, surface);
                half3 original = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                    original = LinearToSRGB(original);
                #endif

                // The FBX shares one atlas across plaster, timber and tiles.
                // Select its light yellow plaster pixels on vertical faces only.
                float3 normalOS = normalize(TransformWorldToObjectNormal(input.normalWS));
                half vertical = 1.0h - smoothstep(0.25h, 0.65h, abs(normalOS.y));
                half plaster = smoothstep(0.52h, 0.65h, original.g)
                    * smoothstep(0.64h, 0.76h, original.r)
                    * smoothstep(0.09h, 0.17h, original.g - original.b)
                    * vertical * _AgedStrength;

                // Object-space projection follows the model when moved/scaled.
                // Clamp to a clean plaster rectangle in the original house atlas.
                float3 p = saturate((TransformWorldToObject(input.positionWS) - _BoundsMin.xyz) * _BoundsInvSize.xyz);
                float2 wallUV = abs(normalOS.x) > abs(normalOS.z) ? p.zy : p.xy;
                wallUV = wallUV * _AgedMap_ST.xy + _AgedMap_ST.zw;
                half3 aged = SAMPLE_TEXTURE2D(_AgedMap, sampler_AgedMap, wallUV).rgb * _AgedTint.rgb;
                surface.albedo = lerp(surface.albedo, aged, plaster);
                surface.metallic = lerp(surface.metallic, 0.0h, plaster);
                surface.smoothness = lerp(surface.smoothness, 0.12h, plaster);
                surface.normalTS = normalize(lerp(surface.normalTS, half3(0,0,1), plaster * 0.7h));

                InputData lighting;
                InitializeInputData(input, surface.normalTS, lighting);
                half4 color = UniversalFragmentPBR(lighting, surface);
                color.rgb = MixFog(color.rgb, lighting.fogCoord);
                return half4(color.rgb, 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
