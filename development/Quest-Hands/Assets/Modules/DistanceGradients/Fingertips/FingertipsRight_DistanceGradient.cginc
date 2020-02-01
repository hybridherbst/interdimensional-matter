#include "GradientHelpers.cginc"

uniform float4 _FingertipsRight_DistanceGradientCenters[10];
uniform int _FingertipsRight_DistanceGradientCentersLength = 10;
uniform float2 _FingertipsRight_FromToDistance;
uniform float _FingertipsRight_Power;

void _FingertipsRight_DistanceGradient_float (float3 worldPosition, out float distance, out float influence)
{
    distance = 10000; 
    influence = 0;

    // [unroll]
    for(int i = 0; i < _FingertipsRight_DistanceGradientCentersLength; i++) {  
        float4 vec = _FingertipsRight_DistanceGradientCenters[i];
        float dist = length(worldPosition - vec.xyz) / vec.w;
        distance = min(distance, dist);

        float pointInfluence = pow(saturate(Remap(dist, _FingertipsRight_FromToDistance.x, _FingertipsRight_FromToDistance.y, 0, 1)), _FingertipsRight_Power);
        influence += pointInfluence;
    }
}