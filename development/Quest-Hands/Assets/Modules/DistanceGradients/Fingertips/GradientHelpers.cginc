#ifndef GRADIENT_HELPERS
#define GRADIENT_HELPERS

float Remap(float val, float srcMin, float srcMax, float dstMin, float dstMax) {
    return (val - srcMin) / (srcMax - srcMin) * (dstMax - dstMin) + dstMin; 
}

#endif