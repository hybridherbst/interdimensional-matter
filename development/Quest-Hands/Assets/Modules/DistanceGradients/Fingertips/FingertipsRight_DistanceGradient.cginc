uniform float4 _FingertipsRight_DistanceGradientCenters[200];
uniform int _FingertipsRight_DistanceGradientCentersLength = 200;

void _FingertipsRight_DistanceGradient_float (float3 worldPosition, out float distance)
{
    float distanceMask = 100000;

    // [unroll]
    for(int i = 0; i < _FingertipsRight_DistanceGradientCentersLength; i++) {  
        float4 vec = _FingertipsRight_DistanceGradientCenters[i];
        distanceMask = min(distanceMask, length(worldPosition - vec.xyz) / vec.w);
    }

    distance = distanceMask;
}