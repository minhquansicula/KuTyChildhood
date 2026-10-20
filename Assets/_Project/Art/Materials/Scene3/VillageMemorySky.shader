Shader "KuTy/VillageMemorySky"
{
    Properties { _Top("Blue sky",Color)=(.22,.38,.5,1) _Horizon("Warm horizon",Color)=(.75,.65,.51,1) }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            float4 _Top,_Horizon;
            struct A { float4 vertex:POSITION; };
            struct V { float4 vertex:SV_POSITION; float3 direction:TEXCOORD0; };
            V Vert(A i) { V o;o.vertex=UnityObjectToClipPos(i.vertex);o.direction=i.vertex.xyz;return o; }
            half4 Frag(V i):SV_Target
            {
                float3 d=normalize(i.direction);
                float h=saturate(d.y);
                half3 c=lerp(_Horizon.rgb,_Top.rgb,pow(h,.5));
                float3 sun=normalize(float3(-.65,.36,-.45));
                c+=half3(.8,.57,.25)*pow(saturate(dot(d,sun)),280)*.35;
                return half4(c,1);
            }
            ENDHLSL
        }
    }
}
