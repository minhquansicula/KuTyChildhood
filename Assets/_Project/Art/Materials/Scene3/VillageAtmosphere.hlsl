#ifndef VILLAGE_ATMOSPHERE_INCLUDED
#define VILLAGE_ATMOSPHERE_INCLUDED
float4 _VillageFogColor,_VillageFogParameters;
half3 VillageHeightFog(half3 color,float3 world)
{
    float3 ray=world-_WorldSpaceCameraPos;float distance=length(ray);
    float vertical=_VillageFogParameters.y*ray.y;
    float integral=abs(vertical)<.01?1-vertical*.5:(1-exp(clamp(-vertical,-30,30)))/vertical;
    float optical=_VillageFogParameters.x*distance*exp(clamp(-_VillageFogParameters.y*(_WorldSpaceCameraPos.y-_VillageFogParameters.z),-20,20))*integral;
    float amount=(1-exp(-max(0,optical)))*_VillageFogParameters.w;
    return lerp(color,_VillageFogColor.rgb,saturate(amount));
}
#endif
