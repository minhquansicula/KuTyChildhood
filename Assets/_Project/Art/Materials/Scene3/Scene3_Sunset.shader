Shader "Scene3/SunsetSky" {
SubShader { Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" } Cull Off ZWrite Off
Pass { HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct V { float4 vertex:POSITION; }; struct F { float4 pos:SV_POSITION;float3 dir:TEXCOORD0; };
F vert(V v){F o;o.pos=UnityObjectToClipPos(v.vertex);o.dir=v.vertex.xyz;return o;}
float4 frag(F i):SV_Target { float3 d=normalize(i.dir);float h=saturate(d.y);float3 horizon=float3(1.0,.49,.24);float3 upper=float3(.26,.23,.40);float3 c=lerp(horizon,upper,pow(h,.55));float3 sun=normalize(float3(-.32,.11,.94));float q=dot(d,sun);c+=float3(1,.42,.16)*pow(saturate(q),90)*.5;c=lerp(c,float3(1,.90,.58),smoothstep(.9988,.9993,q));float bands=sin(d.y*70+sin(d.x*13)*.5);float cloud=smoothstep(.79,.99,bands)*smoothstep(.05,.15,h)*(1-smoothstep(.24,.38,h));c=lerp(c,float3(.71,.38,.37),cloud*.22);return float4(c,1);}
ENDHLSL } } Fallback Off }
