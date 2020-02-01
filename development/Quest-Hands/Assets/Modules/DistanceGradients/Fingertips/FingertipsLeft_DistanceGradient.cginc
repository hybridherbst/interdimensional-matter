uniform float4 _FingertipsLeft_DistanceGradientCenters[200];
uniform int _FingertipsLeft_DistanceGradientCentersLength = 200;

void _FingertipsLeft_DistanceGradient_float (float3 worldPosition, out float distance)
{
    float distanceMask = 100000;

    // [unroll]
    for(int i = 0; i < _FingertipsLeft_DistanceGradientCentersLength; i++) {  
        float4 vec = _FingertipsLeft_DistanceGradientCenters[i];
        distanceMask = min(distanceMask, length(worldPosition - vec.xyz) / vec.w);
    }

    distance = distanceMask;
}