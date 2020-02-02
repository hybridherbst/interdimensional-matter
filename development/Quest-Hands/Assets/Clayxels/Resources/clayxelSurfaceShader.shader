
Shader "Clayxel/ClayxelSurfaceShader"
{
	SubShader
	{
		ZWrite Off // shadow pass, normal oriented billboards         

		Tags { "Queue" = "Geometry" "RenderType"="Opaque" }

		CGPROGRAM
		
		#pragma surface surf Standard vertex:vert addshadow fullforwardshadows
		#pragma target 3.0

		#include "UnityCG.cginc"
		#include "clayxelShadingUtils.cginc"

		#if defined (SHADER_API_D3D11) || defined(SHADER_API_METAL)
		uniform StructuredBuffer<int4> chunkPoints;
		#endif

		float4x4 objectMatrix;
		float3 chunkCenter;
		float chunkSize = 0.0;
		float splatRadius = 0.01;
		float splatSizeMult = 1.0;

		float _Smoothness;
		float _Metallic;
		sampler2D _MainTex;

		struct VertexData{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 tangent : TANGENT;
			float4 color : COLOR;
			float4 texcoord1 : TEXCOORD1;
			float4 texcoord2 : TEXCOORD2;
			uint vid : SV_VertexID;
		};

		struct Input
		{
			float2 tex : TEXCOORD0;
			float4 color : COLOR;
		};

		void vert(inout VertexData outVertex, out Input outData){
			UNITY_INITIALIZE_OUTPUT(Input, outData);
						// in order to shade the point cloud coming from the compute shader, we need to decompress the data.
			uint vertexOffset = outVertex.vid % (3);
			int4 clayxelPointData = int4(0, 0, 0, 0);

			#if defined(SHADER_API_D3D11) || defined(SHADER_API_METAL)
			clayxelPointData = chunkPoints[outVertex.vid / 3];
			#endif

			float3 normal = mul(objectMatrix, unpackNormal(clayxelPointData.z));
			outVertex.normal = normal;

			int4 compressedData = unpackInt4(clayxelPointData.x);

			outVertex.color = float4(unpackRgb(clayxelPointData.w), 1.0);

			float cellSize = chunkSize / 256;
			float3 cellLocalOffset = unpackFloat3(clayxelPointData.y) * cellSize;
			float3 pointPos = expandGridPoint(compressedData.xyz, cellSize, chunkSize) + cellLocalOffset + chunkCenter;

			float4 p = mul(objectMatrix, float4(pointPos, 1.0));

			float newSplatSize = splatRadius * splatSizeMult;
			float3 camUpVec = float3(unity_CameraToWorld[0][1], unity_CameraToWorld[1][1], unity_CameraToWorld[2][1]);
			float3 camSideVec = float3(unity_CameraToWorld[0][0], unity_CameraToWorld[1][0], unity_CameraToWorld[2][0]);

			float3 normalSideVec = normalize(cross(camUpVec, normal)) * (newSplatSize*2.0);
			float3 normalUpVec = normalize(cross(normalSideVec, normal)) * newSplatSize;

			expandSplatVertex(vertexOffset, p, normalUpVec, normalSideVec, outVertex.vertex, outData.tex);
		}

		void surf(Input IN, inout SurfaceOutputStandard o){
			if(length(IN.tex-0.5) > 0.5){// if outside circle
				discard;
			}

			o.Albedo = IN.color * 0.5;
		}

		ENDCG

		ZWrite On // splatting pass, no shadows          

		CGPROGRAM

		#pragma multi_compile SPLATTEXTURE_ON SPLATTEXTURE_OFF
		#pragma surface surf Standard vertex:vert 
		#pragma target 3.0

		#include "UnityCG.cginc"
		#include "clayxelShadingUtils.cginc"

		#if defined (SHADER_API_D3D11) || defined(SHADER_API_METAL)
		uniform StructuredBuffer<int4> chunkPoints;
		#endif

		float4x4 objectMatrix;
		float3 chunkCenter;
		float chunkSize = 0.0;
		float splatRadius = 0.01;
		int solidHighlightId = -1;
		float normalOrientedSplat = 0.0;
		float splatSizeMult = 1.0;

		float _Smoothness;
		float _Metallic;
		float4 _Emission;
		sampler2D _MainTex;

		struct VertexData{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 tangent : TANGENT;
			float4 color : COLOR;
			float4 texcoord1 : TEXCOORD1;
			float4 texcoord2 : TEXCOORD2;
			uint vid : SV_VertexID;
		};

		struct Input
		{
			float2 tex : TEXCOORD0;
			float4 color : COLOR;
		};

		void vert(inout VertexData outVertex, out Input outData)
		{
			UNITY_INITIALIZE_OUTPUT(Input, outData);

						// in order to shade the point cloud coming from the compute shader, we need to decompress the data.
			uint vertexOffset = outVertex.vid % (3);
			int4 clayxelPointData = int4(0, 0, 0, 0);

			#if defined(SHADER_API_D3D11) || defined(SHADER_API_METAL)
			clayxelPointData = chunkPoints[outVertex.vid / 3];
			#endif

			float3 normal = mul(objectMatrix, unpackNormal(clayxelPointData.z));
			outVertex.normal = normal;

			int4 compressedData = unpackInt4(clayxelPointData.x);

			float cellSize = chunkSize / 256;
			float3 cellLocalOffset = unpackFloat3(clayxelPointData.y) * cellSize;
			float3 pointPos = expandGridPoint(compressedData.xyz, cellSize, chunkSize) + cellLocalOffset + chunkCenter;

			float4 p = mul(objectMatrix, float4(pointPos, 1.0));

			outVertex.color = float4(unpackRgb(clayxelPointData.w), 1.0);

			int solidId = compressedData.w - 1;
			if(solidId == solidHighlightId){
				outVertex.color *= 2.0;
			}

			float3 camUpVec = float3(unity_CameraToWorld[0][1], unity_CameraToWorld[1][1], unity_CameraToWorld[2][1]);
			float3 camSideVec = float3(unity_CameraToWorld[0][0], unity_CameraToWorld[1][0], unity_CameraToWorld[2][0]);

			float newSplatSize = splatRadius * splatSizeMult;

			float3 upVec;
			float3 sideVec;
			if(normalOrientedSplat == 0.0){// billboard splats
				upVec = camUpVec * (newSplatSize);
				sideVec = camSideVec * (newSplatSize * 2.0);
			}
			else{// normal oriented splats
				float3 normalSideVec = normalize(cross(camUpVec, normal));
				float3 normalUpVec = normalize(cross(normalSideVec, normal));
				
				if(normalOrientedSplat == 1.0){// fully normal oriented
					upVec = normalUpVec * (newSplatSize);
					sideVec = normalSideVec * (newSplatSize * 2.0);
				}
				else{// interpolated normal orient
					upVec = normalize(lerp(camUpVec, normalUpVec, normalOrientedSplat)) * (newSplatSize);
					sideVec = normalize(lerp(camSideVec, normalSideVec, normalOrientedSplat)) * (newSplatSize*2.0);
				}
			}

			float3 viewVec = float3(unity_CameraToWorld[0][2], unity_CameraToWorld[1][2], unity_CameraToWorld[2][2]);

			#if SPLATTEXTURE_ON // rotate each triangle around viewVec to make it look more natural
				if(normalOrientedSplat == 1.0){
					expandSplatVertex(vertexOffset, p, upVec, sideVec, outVertex.vertex, outData.tex);
				}
				else{
					// expand vert while rotating it to add natural randomness to each textured splat
					float rotVal = dot(pointPos, pointPos*10.0);
					
					if(vertexOffset == 0){
						outVertex.vertex = float4(p + rotatePosition(viewVec, rotVal, ((-upVec) + sideVec)), 1.0);
						outData.tex = float2(-0.5, 0.0);
					}
					else if(vertexOffset == 1){
						outVertex.vertex = float4(p + rotatePosition(viewVec, rotVal, ((-upVec) - sideVec)), 1.0);
						outData.tex = float2(1.5, 0.0);
					}
					else if(vertexOffset == 2){
						outVertex.vertex = float4(p + rotatePosition(viewVec, rotVal, (upVec*1.7)), 1.0);
						outData.tex = float2(0.5, 1.35);
					}
				}
			#else // otherwise just create a triangle
				expandSplatVertex(vertexOffset, p, upVec, sideVec, outVertex.vertex, outData.tex);
			#endif

			if(normalOrientedSplat > 0.0){
				// flatten triangle on point' original depth to avoid ugly intersections
				outVertex.vertex = float4(outVertex.vertex.xyz - (viewVec * (dot(viewVec, outVertex.vertex.xyz - p))), 1.0);
			}
		}
		
		void surf(Input IN, inout SurfaceOutputStandard o)
		{ 
			#if SPLATTEXTURE_ON
				float alphaTexture = tex2D(_MainTex, IN.tex).a;
				if(alphaTexture < 1.0){
					if(alphaTexture == 0.0){
						discard;
					}
					else{// fuzz discard
						if(random(IN.tex) > alphaTexture){
							discard;
						}
					}
				}
			#else
				if(length(IN.tex-0.5) > 0.5){// if outside circle
					discard;
				}
			#endif

			o.Albedo = IN.color;
			o.Metallic = _Metallic;
			o.Smoothness = _Smoothness;
			o.Emission = _Emission;
		}

		ENDCG
	}
}