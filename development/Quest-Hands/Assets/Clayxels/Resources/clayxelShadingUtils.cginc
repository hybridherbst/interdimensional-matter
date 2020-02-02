#ifndef CLAYXEL_CG_INCLUDED
#define CLAYXEL_CG_INCLUDED

// Utilities to shade and interpred clayxel data coming from the compute shader

float3 expandGridPoint(int3 cellCoord, float cellSize, float chunkSize){
	float cellCornerOffset = cellSize * 0.5;
	float halfBounds = chunkSize* 0.5;
	float3 gridPoint = float3(
		(cellSize * cellCoord.x) - halfBounds, 
		(cellSize * cellCoord.y) - halfBounds, 
		(cellSize * cellCoord.z) - halfBounds) + cellCornerOffset;

	return gridPoint;
}

fixed3 unpackRgb(uint inVal){
	int r = (inVal & 0x000000FF) >>  0;
	int g = (inVal & 0x0000FF00) >>  8;
	int b = (inVal & 0x00FF0000) >> 16;

	return fixed3(r/255.0, g/255.0, b/255.0);
}

float2 unpackFloat2(float input){
	int precision = 2048;
	float2 output = float2(0.0, 0.0);

	output.y = input % precision;
	output.x = floor(input / precision);

	return output / (precision - 1);
}

float3 unpackNormal(float fSingle){
	float2 f = unpackFloat2(fSingle);

	f = f * 2.0 - 1.0;

	float3 n = float3( f.x, f.y, 1.0 - abs( f.x ) - abs( f.y ) );
	float t = saturate( -n.z );
	n.xy += n.xy >= 0.0 ? -t : t;

	return normalize( n );
}

float3 unpackFloat3(float f){
	return frac(f / float3(16777216, 65536, 256));
}

int4 unpackInt4(uint inVal){
	uint r = inVal >> 24;
	uint g = (0x00FF0000 & inVal) >> 16;
	uint b = (0x0000FF00 & inVal) >> 8;
	uint a = (0x000000FF & inVal);

	return int4(r, g, b, a);
}

float random(float2 uv){
    return frac(sin(dot(uv,float2(12.9898,78.233)))*43758.5453123);
}

float3 rotatePosition(float3 axis, float angle, float3 p)
{
    float3 n = axis; // the axis to rotate about

    float c = cos(angle);
    float s = sin(angle);
    float invC = 1.0 - c;
    float nXs = n.x * s;
    float nYs = n.y * s;
    float nZs = n.z * s;
    
    // Specify the rotation transformation matrix:
    float3x3 m = float3x3(
        n.x*n.x * invC + c, // column 1 of row 1
        n.x*n.y * invC + nZs, // column 2 of row 1
        n.x*n.z * invC - nYs, // column 3 of row 1

        n.y*n.x * invC - nZs, // column 1 of row 2
        n.y*n.y * invC + c,
        n.y*n.z * invC + nXs,

        n.z*n.x * invC + nYs, // column 1 of row 3
        n.z*n.y * invC - nXs,
        n.z*n.z * invC + c);

    // Apply the rotation to our 3D position:
    float3 q = mul(m,p);
    return q;
}

void expandSplatVertex(int vertexOffset, float3 p, float3 upVec, float3 sideVec, out float4 vertex, out float2 tex){
	if(vertexOffset == 0){
		vertex = float4(p + ((-upVec) + sideVec), 1.0);
		tex = float2(-0.5, 0.0);
	}
	else if(vertexOffset == 1){
		vertex = float4(p + ((-upVec) - sideVec), 1.0);
		tex = float2(1.5, 0.0);
	}
	else if(vertexOffset == 2){
		vertex = float4(p + (upVec*1.7), 1.0);
		tex = float2(0.5, 1.35);
	}
}

void expandSplatVertexClipped(int vertexOffset, float3 p, float3 upVec, float3 sideVec, out float4 vertex, out float2 tex){
	if(vertexOffset == 0){
		vertex = UnityObjectToClipPos(float4(p + ((-upVec) + sideVec), 1.0));
		tex = float2(-0.5, 0.0);
	}
	else if(vertexOffset == 1){
		vertex = UnityObjectToClipPos(float4(p + ((-upVec) - sideVec), 1.0));
		tex = float2(1.5, 0.0);
	}
	else if(vertexOffset == 2){
		vertex = UnityObjectToClipPos(float4(p + (upVec*1.7), 1.0));
		tex = float2(0.5, 1.35);
	}
}

#endif // INCLUDED
