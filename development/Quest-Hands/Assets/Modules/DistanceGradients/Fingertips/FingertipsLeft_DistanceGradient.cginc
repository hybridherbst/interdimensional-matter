#include "GradientHelpers.cginc"

uniform float4 _FingertipsLeft_DistanceGradientCenters[10];
uniform int _FingertipsLeft_DistanceGradientCentersLength = 10;
uniform float2 _FingertipsLeft_FromToDistance;
uniform float _FingertipsLeft_Power;

void _FingertipsLeft_DistanceGradient_float (float3 worldPosition, out float distance, out float influence)
{
    distance = 10000;
    influence = 0;

    // [unroll]
    for(int i = 0; i < _FingertipsLeft_DistanceGradientCentersLength; i++) {  
        float4 vec = _FingertipsLeft_DistanceGradientCenters[i];
        float dist = length(worldPosition - vec.xyz) / vec.w;
        distance = min(distance, dist);

        float pointInfluence = pow(saturate(Remap(dist, _FingertipsLeft_FromToDistance.x, _FingertipsLeft_FromToDistance.y, 0, 1)), _FingertipsLeft_Power);
        influence += pointInfluence;
    }
}