Shader "KuTy/Village/Rice Instanced Wind"
{
    Properties
    {
        [MainTexture] _BaseMap("Rice atlas", 2D) = "white" {}
        [Normal] _BumpMap("Rice normal atlas", 2D) = "bump" {}
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)
        _Cutoff("Alpha cutoff", Range(0,1)) = .3
        _NormalStrength("Normal strength", Range(0,1)) = .55
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
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST, _BaseColor;
        float _Cutoff, _NormalStrength, _WindAmplitude, _WindSpeed;
        float4 _WindDirection;
        float _GustAmplitude, _GustInterval, _GustTravelSpeed, _GustWidth, _WindPreviewTime;
        CBUFFER_END
        float4 _VillageRiceInteractor;
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
            float mask = input.color.r * input.color.r;
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
            #pragma multi_compile_instancing
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
                return output;
            }
            half4 RiceFragment(Varyings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv) * _BaseColor;
                clip(albedo.a - _Cutoff);
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,input.uv),_NormalStrength);
                half3 normal = normalize(input.normalWS);
                half3 tangent = input.tangentWS.xyz;
                half3 bitangent = cross(normal,tangent) * input.tangentWS.w;
                normal = normalize(normalTS.x * tangent + normalTS.y * bitangent + normalTS.z * normal);
                normal *= IS_FRONT_VFACE(frontFace,1,-1);
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half diffuse = saturate((dot(normal,sun.direction) + .4h) / 1.4h);
                half3 lighting = SampleSH(normal) + sun.color * diffuse * sun.shadowAttenuation;
                float3 origin = TransformObjectToWorld(float3(0,0,0));
                half variation = .94h + .10h * frac(sin(dot(origin.xz,float2(12.9898,78.233))) * 43758.5453);
                half3 color = albedo.rgb * max(lighting,.16h) * variation;
                return half4(MixFog(color,input.fog),1);
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
            #pragma multi_compile_instancing
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
                clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).a * _BaseColor.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
