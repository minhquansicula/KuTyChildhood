Shader "KuTy/CountrysideMorningSky"
{
    Properties
    {
        _Horizon ("Horizon", Color) = (0.73, 0.72, 0.67, 1)
        _Zenith ("Upper sky", Color) = (0.40, 0.55, 0.67, 1)
        _CloudMap ("Cloud image", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex : POSITION; };
            struct Output { float4 position : SV_POSITION; float3 direction : TEXCOORD0; };
            float4 _Horizon, _Zenith;
            sampler2D _CloudMap;
            Output vert(Input input)
            {
                Output result;
                result.position = UnityObjectToClipPos(input.vertex);
                result.direction = input.vertex.xyz;
                return result;
            }
            float4 frag(Output input) : SV_Target
            {
                float altitude = saturate(normalize(input.direction).y);
                float3 direction = normalize(input.direction);
                float2 uv = float2(0.5 + atan2(direction.x, direction.z) / 2.2,
                    saturate(altitude * 1.65));
                float4 clouds = tex2D(_CloudMap, uv);
                return lerp(_Horizon, clouds, smoothstep(0, 0.12, altitude));
            }
            ENDHLSL
        }
    }
}
