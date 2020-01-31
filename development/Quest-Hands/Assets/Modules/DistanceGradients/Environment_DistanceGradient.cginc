uniform float4 _Environment_DistanceGradientCenters[200];
uniform int _Environment_DistanceGradientCentersLength = 200;

void _Environment_DistanceGradient_float (float3 worldPosition, out float distance, out float3 direction)
{
    float distanceMask = 1;

    // [unroll]
    int smallestIndex = -1;
    for(int i = 0; i < _Environment_DistanceGradientCentersLength; i++) {  
        float4 vec = _Environment_DistanceGradientCenters[i];
        float len = length(worldPosition - vec.xyz) / vec.w;
        if(len < distanceMask) {
            distanceMask = len;
            smallestIndex = i;
        }
    }

    if(smallestIndex >= 0)
    {
        distance = distanceMask;
        direction = worldPosition - _Environment_DistanceGradientCenters[smallestIndex];
    }
    else {
        distance = 100000;
        direction = float3(0,0,0);
    }
}