uniform float4 _Hands_DistanceGradientCenters[200];
uniform int _Hands_DistanceGradientCentersLength = 200;

void _Hands_DistanceGradient_float (float3 worldPosition, out float distance)
{
    float distanceMask = 1;

    // [unroll]
    for(int i = 0; i < _Hands_DistanceGradientCentersLength; i++) {  
        float4 vec = _Hands_DistanceGradientCenters[i];
        distanceMask = min(distanceMask, length(worldPosition - vec.xyz) / vec.w);
    }

    distance = saturate(1 - distanceMask);
}