Shader "KuTy/Village/Rice Instanced Wind"
{
    Properties
    {
        [MainTexture] _BaseMap("Rice atlas", 2D) = "white" {}
        [Normal] _BumpMap("Rice normal atlas", 2D) = "bump" {}
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)
        _BottomColor("Base Stem Tint", Color) = (0.50, 0.58, 0.28, 1.0)
        _TopColor("Top Panicle Tint", Color) = (1.08, 0.98, 0.70, 1.0)
        _FieldVariation("Field Maturity Variation", Range(0, 1)) = 0.65
        _Cutoff("Alpha cutoff", Range(0,1)) = .3
        _NormalStrength("Normal strength", Range(0,1)) = .55
        _Saturation("Earthy colour saturation",Range(0,1))=.95
        _Brightness("Rice brightness",Range(.3,1.5))=1.05
        _Translucency("Backlight Translucency", Range(0,2)) = 1.0
        _TranslucencyColor("Translucency Tint", Color) = (1.0, 0.82, 0.35, 1.0)
        _NormalUpBlend("Normal Up Blend", Range(0,1)) = 0.35
        _WindAmplitude("Wind amplitude (metres)", Range(0,.3)) = .075
        _WindSpeed("Wind speed", Range(0,3)) = .8
        _WindDirection("Wind direction (XZ)", Vector) = (.8,.6,0,0)
        _GustAmplitude("Gust strength (metres)", Range(0,.5)) = .28
        _GustInterval("Gust interval (seconds)", Range(18,40)) = 24
        _GustTravelSpeed("Gust travel speed (m/s)", Range(8,25)) = 14
        _GustWidth("Gust band width (metres)", Range(4,18)) = 10
        [HideInInspector] _WindPreviewTime("Wind preview time", Float) = -1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Off
        ZWrite On
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "../VillageAtmosphere.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST, _BaseColor;
        float4 _TranslucencyColor;
        float4 _BottomColor, _TopColor;
        float _Cutoff, _NormalStrength, _WindAmplitude, _WindSpeed, _Saturation, _Brightness;
        float _Translucency, _NormalUpBlend, _FieldVariation;
        float4 _WindDirection;
        float _GustAmplitude, _GustInterval, _GustTravelSpeed, _GustWidth, _WindPreviewTime;
        CBUFFER_END
        float4 _VillageRiceInteractor;
        #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
        struct RiceInstance {float4x4 objectToWorld;float4x4 worldToObject;float4 sphere;};
        StructuredBuffer<RiceInstance> _RiceInstances;
        StructuredBuffer<uint> _RiceVisible;
        #endif
        void SetupRiceInstance()
        {
            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
            RiceInstance data=_RiceInstances[_RiceVisible[unity_InstanceID]];
            unity_ObjectToWorld=data.objectToWorld;unity_WorldToObject=data.worldToObject;
            #endif
        }
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            float4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        float GustHash(float value)
        {
            return frac(sin(value * 12.9898 + 78.233) * 43758.5453);
        }
        float3 RicePosition(Attributes input)
        {
            float3 p = TransformObjectToWorld(input.positionOS.xyz);
            float heightWeight = saturate(input.positionOS.y / 0.95);
            float mask = max(input.color.r * input.color.r, heightWeight * heightWeight * heightWeight);
            float time = _WindPreviewTime >= 0 ? _WindPreviewTime : _Time.y;
            float2 direction = dot(_WindDirection.xy,_WindDirection.xy) > .001
                ? normalize(_WindDirection.xy) : float2(.8,.6);
            float speed = max(_GustTravelSpeed,1);
            float width = max(_GustWidth,1);
            // Leave enough time for the band to clear the entire village before changing seed.
            float period = max(_GustInterval,(240 + 2 * width) / speed + 4);
            float cycle = floor(time / period);
            float age = time - cycle * period;
            float randomAngle = (GustHash(cycle + 1) - .5) * 1.3;
            float sine, cosine; sincos(randomAngle,sine,cosine);
            float2 gustDirection = float2(direction.x * cosine - direction.y * sine,
                direction.x * sine + direction.y * cosine);
            float delay = GustHash(cycle + 7) * 3;
            float front = -110 + (age - delay) * speed;
            float band = saturate(1 - abs(dot(p.xz,gustDirection) - front) / width);
            band = band * band * (3 - 2 * band) * step(delay,age);
            float strength = _GustAmplitude * (.7 + .6 * GustHash(cycle + 19)) * band;
            float3 origin = TransformObjectToWorld(float3(0,0,0));
            float phase = dot(p.xz,direction) * .6 + time * _WindSpeed;
            phase += sin(dot(origin.xz,float2(1.7,2.3))) * .35;
            float background = sin(phase) * _WindAmplitude;
            float flutter = sin(phase * 3 + time * (2 + band * 3))
                * (.008 + strength * .12) * input.color.g;
            float gustBend = strength * (1 + .25 * sin(phase * 1.7 + time * 3));
            p.xz += (direction * background + gustDirection * gustBend
                + float2(-gustDirection.y,gustDirection.x) * flutter) * mask;
            p.y -= min(gustBend * gustBend * .65,.1) * mask;
            float2 delta = p.xz - _VillageRiceInteractor.xz;
            float distance = length(delta);
            float interaction = saturate(1 - distance / max(_VillageRiceInteractor.w,.001));
            interaction *= step(.01,_VillageRiceInteractor.w);
            p.xz += delta / max(distance,.05) * interaction * .2 * mask;
            return p;
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex RiceVertex
            #pragma fragment RiceFragment
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:SetupRiceInstance
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                float fog : TEXCOORD4;
                float localHeight : TEXCOORD5;
                float3 plantOrigin : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings RiceVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input,output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = RicePosition(input);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS,input.tangentOS);
                output.normalWS = normals.normalWS;
                output.tangentWS = float4(normals.tangentWS,input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv,_BaseMap);
                output.fog = ComputeFogFactor(output.positionCS.z);
                output.localHeight = max(input.color.r, saturate(input.positionOS.y / 0.95));
                output.plantOrigin = TransformObjectToWorld(float3(0,0,0));
                return output;
            }
            half4 RiceFragment(Varyings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv) * _BaseColor;
                
                // Anti-aliased alpha testing: scales cutoff with UV derivative footprint so thin leaves
                // and grain panicles do not disintegrate into noisy speckles ("ro ro") at medium/far distance.
                float2 ddxUv = ddx(input.uv);
                float2 ddyUv = ddy(input.uv);
                float mipFootprint = max(dot(ddxUv, ddxUv), dot(ddyUv, ddyUv));
                float mipFactor = saturate(sqrt(mipFootprint) * 50.0);
                float adaptiveCutoff = max(0.12, _Cutoff - 0.14 * mipFactor);
                clip(albedo.a - adaptiveCutoff);
                
                half luminance = dot(albedo.rgb, half3(0.2126, 0.7152, 0.0722));
                albedo.rgb = lerp(luminance.xxx, albedo.rgb, _Saturation) * _Brightness;

                // Botanical height-based color gradient:
                // Deep olive-green stem base -> golden-yellow green -> ripe harvest gold at panicle tips
                half heightNorm = saturate(input.localHeight);
                half3 botanicalGradient = lerp(_BottomColor.rgb, _TopColor.rgb, smoothstep(0.12h, 0.82h, heightNorm));

                // Macro spatial maturity wave across the paddy fields
                half fieldWave = sin(input.plantOrigin.x * 0.08h + input.plantOrigin.z * 0.06h) * cos(input.plantOrigin.x * 0.05h - input.plantOrigin.z * 0.07h);
                half3 maturityTint = lerp(half3(0.92, 0.96, 0.82), half3(1.06, 0.98, 0.86), saturate(fieldWave * 0.5h + 0.5h));

                // Micro per-clump seed for natural variation
                half clumpSeed = frac(sin(dot(input.plantOrigin.xz, half2(12.9898, 78.233))) * 43758.5453);
                half3 clumpVariation = lerp(half3(1,1,1), maturityTint, _FieldVariation) * (0.94h + 0.12h * clumpSeed);

                albedo.rgb *= botanicalGradient * clumpVariation;

                half3 normal = normalize(input.normalWS);
                if (_NormalStrength > 0.01 && length(input.tangentWS.xyz) > 0.1)
                {
                    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,input.uv),_NormalStrength);
                    half3 tangent = normalize(input.tangentWS.xyz);
                    half3 bitangent = cross(normal,tangent) * input.tangentWS.w;
                    normal = normalize(normalTS.x * tangent + normalTS.y * bitangent + normalTS.z * normal);
                }
                normal *= IS_FRONT_VFACE(frontFace, 1, -1);

                // Blend normal upwards to give foliage soft, fluffy volume and eliminate harsh polygon cuts
                half3 softNormal = normalize(lerp(normal, half3(0, 1, 0), _NormalUpBlend));

                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);

                // Two-sided wrap diffuse for thin leaves (energy-conserved wrap)
                half NdotL = dot(softNormal, sun.direction);
                half wrap = (NdotL + 0.35h) / 1.35h;
                half diffuseFront = saturate(wrap);
                half leafThinness = saturate(input.localHeight / 1.1h);
                half diffuseBack = saturate(-wrap) * _Translucency * (0.3h + 0.5h * leafThinness);

                // Subtle backlight rim when looking into sunset
                half VdotL = saturate(dot(viewDir, -sun.direction));
                half rim = pow(VdotL, 4.0h) * 0.35h * _Translucency;

                // Soft foliage shadowing: prevent leaf undersides from dropping into harsh pitch-black noise
                half shadow = lerp(0.35h, 1.0h, sun.shadowAttenuation);
                half3 directLighting = sun.color * (diffuseFront + (diffuseBack + rim) * _TranslucencyColor.rgb) * shadow;

                // Ambient light with sky hemisphere & warm paddy mud bounce
                half3 ambient = SampleSH(softNormal);
                half groundBounce = saturate(1.0h - input.localHeight / 0.65h);
                ambient += half3(0.24, 0.18, 0.10) * groundBounce * 0.40h;
                ambient = max(ambient, half3(0.16, 0.14, 0.09));

                half3 lighting = ambient + directLighting;
                half3 color = albedo.rgb * lighting;
                return half4(lerp(MixFog(color,input.fog),VillageHeightFog(color,input.positionWS),_VillageFogParameters.w),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:SetupRiceInstance
            struct DepthVaryings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            DepthVaryings DepthVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                DepthVaryings output;
                output.positionCS = TransformWorldToHClip(RicePosition(input));
                output.uv = TRANSFORM_TEX(input.uv,_BaseMap);
                return output;
            }
            half4 DepthFragment(DepthVaryings input) : SV_Target
            {
                float2 ddxUv = ddx(input.uv);
                float2 ddyUv = ddy(input.uv);
                float mipFootprint = max(dot(ddxUv, ddxUv), dot(ddyUv, ddyUv));
                float mipFactor = saturate(sqrt(mipFootprint) * 50.0);
                float adaptiveCutoff = max(0.12, _Cutoff - 0.14 * mipFactor);
                clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).a * _BaseColor.a - adaptiveCutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
