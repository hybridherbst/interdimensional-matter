
// #define DEBUGGRIDPOINTS

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.SceneManagement;
#endif

class ClayxelChunk{
	public ComputeBuffer chunkOutPointsBuffer;
	public ComputeBuffer clayxelShaderArgs;
	public Vector3 center = new Vector3();
	public Material clayxelMaterial;
	public Material clayxelPickingMaterial;

	#if DEBUGGRIDPOINTS
		public ComputeBuffer debugGridOutPointsBuffer;
	#endif
}

[ExecuteInEditMode]
public class Clayxel : MonoBehaviour{
	public bool showContainer = true;
	public int chunkSize = 8;
	public int numChunksX = 1;
	public int numChunksY = 1;
	public int numChunksZ = 1;
	public float normalOrientedSplat = 0.0f;
	public Texture2D splatTexture = null;
	public float splatSizeMultiplier = 1.0f;

	static public bool globalDataNeedsInit = true;

	static List<string> solidsCatalogueLabels = new List<string>();
	static List<List<string[]>> solidsCatalogueParameters = new List<List<string[]>>();
	static ComputeShader claycoreCompute;
	static ComputeBuffer chunkCellBuffer;
	static ComputeBuffer chunkOutDataBuffer;
	static ComputeBuffer solidsFilterInBuffer;
	static ComputeBuffer solidsFilterOutBuffer;
	static ComputeBuffer triangleConnectionTable;
	static List<ComputeBuffer> globalCompBuffers = new List<ComputeBuffer>();
	static int lastUpdatedContainerId = -1;
	static int maxThreads = 8;

	public float materialSmoothness = 0.5f;
	public float materialMetallic = 0.5f;
	public Color materialEmission = new Color(0.0f, 0.0f, 0.0f, 1.0f);
	
	public bool needsUpdate = true;

	ComputeBuffer solidsPosBuffer;
	ComputeBuffer solidsRotBuffer;
	ComputeBuffer solidsScaleBuffer;
	ComputeBuffer solidsBlendBuffer;
	ComputeBuffer solidsTypeBuffer;
	ComputeBuffer solidsColorBuffer;
	ComputeBuffer solidsAttrsBuffer;
	
	List<Vector3> solidsPos;
	List<Quaternion> solidsRot;
	List<Vector3> solidsScale;
	List<float> solidsBlend;
	List<int> solidsType;
	List<Vector3> solidsColor;
	List<Vector4> solidsAttrs;
	List<ClayxelChunk> chunks = new List<ClayxelChunk>();
	List<ComputeBuffer> compBuffers = new List<ComputeBuffer>();
	int chunkMaxOutPoints = (256*256*256) / 8;
	bool needsInit = true;
	bool invalidated = false;
	int[] countBufferArray = new int[1]{0};
	ComputeBuffer countBuffer;
	Vector3 boundsScale = new Vector3(0.0f, 0.0f, 0.0f);
	Vector3 boundsCenter = new Vector3(0.0f, 0.0f, 0.0f);
	Bounds renderBounds = new Bounds();
	Vector3[] vertices = new Vector3[1];
	int[] meshTopology = new int[1];
	bool solidsContainerNeedsUpdate = false;
	List<WeakReference> clayObjects = new List<WeakReference>();
	int numChunks = 0;
	float deltaTime = 0.0f;
	bool meshCached = false;
	Mesh mesh = null;
	int numThreadsCompute4;
	int numThreadsCompute6;
	bool forceUpdate = false;
	float splatRadius = 0.0f;

	enum Kernels{
		compute0,
		compute2,
		compute4,
		compute6,
		cache4,
		cache6,
		genPointCloud,
		clearChunkData,
		debugDisplayGridPoints,
		genMesh
	}

	void Awake(){
		Clayxel.globalDataNeedsInit = true;
		this.needsInit = true;
	}

	void OnDestroy(){
		this.invalidated = true;

		this.releaseBuffers();

		if(UnityEngine.Object.FindObjectsOfType<Clayxel>().Length == 0){
			Clayxel.releaseGlobalBuffers();
		}

		#if UNITY_EDITOR
			if(!Application.isPlaying){
				this.removeEditorEvents();
			}
		#endif
	}

	void releaseBuffers(){
		for(int i = 0; i < this.compBuffers.Count; ++i){
			this.compBuffers[i].Release();
		}

		this.compBuffers.Clear();
	}

	static void releaseGlobalBuffers(){
		for(int i = 0; i < Clayxel.globalCompBuffers.Count; ++i){
			Clayxel.globalCompBuffers[i].Release();
		}

		Clayxel.globalCompBuffers.Clear();
	}

	public void forceClayUpdate(){
		this.forceUpdate = true;
	}

	void limitChunkValues(){
		if(this.numChunksX > 4){
			this.numChunksX = 4;
		}
		if(this.numChunksY > 4){
			this.numChunksY = 4;
		}
		if(this.numChunksZ > 4){
			this.numChunksZ = 4;
		}
		if(this.numChunksX < 1){
			this.numChunksX = 1;
		}
		if(this.numChunksY < 1){
			this.numChunksY = 1;
		}
		if(this.numChunksZ < 1){
			this.numChunksZ = 1;
		}

		if(this.chunkSize < 4){
			this.chunkSize = 4;
		}
	}

	static public void initGlobalData(){
		if(!Clayxel.globalDataNeedsInit){
			return;
		}

		Clayxel.reloadSolidsCatalogue();

		#if UNITY_EDITOR
			if(!Application.isPlaying){
				Clayxel.setupSceneViewer();
			}
		#endif

		Clayxel.globalDataNeedsInit = false;

		Clayxel.lastUpdatedContainerId = -1;

		Clayxel.releaseGlobalBuffers();

		UnityEngine.Object clayCore = Resources.Load("clayCoreLock");
		if(clayCore == null){
			clayCore = Resources.Load("clayCore");
			Debug.Log("loaded unlocked core");
		}

		int chunkPoints = 256 * 256 * 256;

		Clayxel.claycoreCompute = (ComputeShader)Instantiate(clayCore);

		Clayxel.chunkCellBuffer = new ComputeBuffer(chunkPoints, sizeof(int)*2);
		Clayxel.globalCompBuffers.Add(Clayxel.chunkCellBuffer);

		Clayxel.chunkOutDataBuffer = new ComputeBuffer(chunkPoints, sizeof(float) * 3);
		Clayxel.globalCompBuffers.Add(Clayxel.chunkOutDataBuffer);
		
		int maxFilters = 64 * 64 * 64;
		Clayxel.solidsFilterInBuffer = new ComputeBuffer(maxFilters, sizeof(int) * 4);
		Clayxel.globalCompBuffers.Add(Clayxel.solidsFilterInBuffer);

		Clayxel.solidsFilterOutBuffer = new ComputeBuffer(maxFilters, sizeof(int) * 4);
		Clayxel.globalCompBuffers.Add(Clayxel.solidsFilterOutBuffer);

		Clayxel.triangleConnectionTable = new ComputeBuffer(256 * 16, sizeof(int));
		Clayxel.globalCompBuffers.Add(Clayxel.triangleConnectionTable);

		Clayxel.triangleConnectionTable.SetData(MarchingCubesTables.TriangleConnectionTable);

		int numKernels = Enum.GetNames(typeof(Kernels)).Length;
		for(int i = 0; i < numKernels; ++i){
			Clayxel.claycoreCompute.SetBuffer(i, "chunkCell", Clayxel.chunkCellBuffer);
			Clayxel.claycoreCompute.SetBuffer(i, "chunkOutData", Clayxel.chunkOutDataBuffer);

			Clayxel.claycoreCompute.SetBuffer(i, "solidsFilterIn", Clayxel.solidsFilterInBuffer);
			Clayxel.claycoreCompute.SetBuffer(i, "solidsFilterOut", Clayxel.solidsFilterOutBuffer);
		}
		
		Clayxel.claycoreCompute.SetBuffer((int)Kernels.genPointCloud, "triangleConnectionTable", Clayxel.triangleConnectionTable);
		Clayxel.claycoreCompute.SetBuffer((int)Kernels.genMesh, "triangleConnectionTable", Clayxel.triangleConnectionTable);

		#if DEBUGGRIDPOINTS
			Clayxel.claycoreCompute.SetBuffer((int)Kernels.debugDisplayGridPoints, "chunkCell", Clayxel.chunkCellBuffer);
			Clayxel.claycoreCompute.SetBuffer((int)Kernels.debugDisplayGridPoints, "chunkOutData", Clayxel.chunkOutDataBuffer);
		#endif
	}

	public void init(){
		#if UNITY_EDITOR
			if(!Application.isPlaying){
				this.reinstallEditorEvents();
			}
		#endif

		if(Clayxel.globalDataNeedsInit){
			Clayxel.initGlobalData();
		}

		this.needsInit = false;

		this.limitChunkValues();

		this.clayObjects.Clear();

		this.releaseBuffers();

		this.numThreadsCompute4 = 16 / Clayxel.maxThreads;
		this.numThreadsCompute6 = 64 / Clayxel.maxThreads;

		int maxSolids = 128;
		this.solidsPosBuffer = new ComputeBuffer(maxSolids, sizeof(float) * 3);
		this.compBuffers.Add(this.solidsPosBuffer);
		this.solidsRotBuffer = new ComputeBuffer(maxSolids, sizeof(float) * 4);
		this.compBuffers.Add(this.solidsRotBuffer);
		this.solidsScaleBuffer = new ComputeBuffer(maxSolids, sizeof(float) * 3);
		this.compBuffers.Add(this.solidsScaleBuffer);
		this.solidsBlendBuffer = new ComputeBuffer(maxSolids, sizeof(float));
		this.compBuffers.Add(this.solidsBlendBuffer);
		this.solidsTypeBuffer = new ComputeBuffer(maxSolids, sizeof(int));
		this.compBuffers.Add(this.solidsTypeBuffer);
		this.solidsColorBuffer = new ComputeBuffer(maxSolids, sizeof(float) * 3);
		this.compBuffers.Add(this.solidsColorBuffer);
		this.solidsAttrsBuffer = new ComputeBuffer(maxSolids, sizeof(float) * 4);
		this.compBuffers.Add(this.solidsAttrsBuffer);
		
		this.solidsPos = new List<Vector3>(new Vector3[maxSolids]);
		this.solidsRot = new List<Quaternion>(new Quaternion[maxSolids]);
		this.solidsScale = new List<Vector3>(new Vector3[maxSolids]);
		this.solidsBlend = new List<float>(new float[maxSolids]);
		this.solidsType = new List<int>(new int[maxSolids]);
		this.solidsColor = new List<Vector3>(new Vector3[maxSolids]);
		this.solidsAttrs = new List<Vector4>(new Vector4[maxSolids]);

		this.splatRadius = (((float)this.chunkSize / 256) * 0.5f) * 1.7f;

		this.initChunks();
		this.updateSplatLook();

		this.countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
		this.compBuffers.Add(this.countBuffer);

		this.solidsContainerNeedsUpdate = true;
		this.needsUpdate = true;
		Clayxel.lastUpdatedContainerId = -1;

		this.updateChunksTransform();
	}

	void initChunks(){
		this.numChunks = 0;
		this.chunks.Clear();

		this.boundsScale.x = (float)this.chunkSize * this.numChunksX;
		this.boundsScale.y = (float)this.chunkSize * this.numChunksY;
		this.boundsScale.z = (float)this.chunkSize * this.numChunksZ;

		float gridCenterOffset = (this.chunkSize * 0.5f);
		this.boundsCenter.x = ((this.chunkSize * (this.numChunksX - 1)) * 0.5f) - (gridCenterOffset*(this.numChunksX-1));
		this.boundsCenter.y = ((this.chunkSize * (this.numChunksY - 1)) * 0.5f) - (gridCenterOffset*(this.numChunksY-1));
		this.boundsCenter.z = ((this.chunkSize * (this.numChunksZ - 1)) * 0.5f) - (gridCenterOffset*(this.numChunksZ-1));

		for(int x = 0; x < this.numChunksX; ++x){
			for(int y = 0; y < this.numChunksY; ++y){
				for(int z = 0; z < this.numChunksZ; ++z){
					this.initNewChunk(x, y, z);
					this.numChunks += 1;
				}
			}
		}
	}

	void initNewChunk(int x, int y, int z){
		ClayxelChunk chunk = new ClayxelChunk();
		this.chunks.Add(chunk);

		float seamOffset = this.chunkSize / 256.0f; // removes the seam between chunks
		float chunkOffset = this.chunkSize - seamOffset;
		float gridCenterOffset = (this.chunkSize * 0.5f);
		chunk.center = new Vector3(
			(-((this.chunkSize * this.numChunksX) * 0.5f) + gridCenterOffset) + (chunkOffset * x),
			(-((this.chunkSize * this.numChunksY) * 0.5f) + gridCenterOffset) + (chunkOffset * y),
			(-((this.chunkSize * this.numChunksZ) * 0.5f) + gridCenterOffset) + (chunkOffset * z));

		chunk.chunkOutPointsBuffer = new ComputeBuffer(this.chunkMaxOutPoints, sizeof(int) * 4, ComputeBufferType.Counter);
		this.compBuffers.Add(chunk.chunkOutPointsBuffer);

		chunk.clayxelShaderArgs = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
		this.compBuffers.Add(chunk.clayxelShaderArgs);

		chunk.clayxelShaderArgs.SetData(new int[]{0, 1, 0, 0});

		chunk.clayxelMaterial = new Material(Shader.Find("Clayxel/ClayxelSurfaceShader"));
		chunk.clayxelPickingMaterial = new Material(Shader.Find("Clayxel/ClayxelPickingShader"));

		chunk.clayxelMaterial.SetBuffer("chunkPoints", chunk.chunkOutPointsBuffer);

		chunk.clayxelPickingMaterial.SetBuffer("chunkPoints", chunk.chunkOutPointsBuffer);

		#if DEBUGGRIDPOINTS
			chunk.clayxelMaterial = new Material(Shader.Find("Clayxel/ClayxelDebugShader"));

			chunk.debugGridOutPointsBuffer = new ComputeBuffer(this.chunkMaxOutPoints, sizeof(float)*3, ComputeBufferType.Counter);
			this.compBuffers.Add(chunk.debugGridOutPointsBuffer);

			chunk.clayxelMaterial.SetBuffer("debugChunkPoints", chunk.debugGridOutPointsBuffer);
		#endif

		chunk.clayxelMaterial.SetFloat("splatRadius", this.splatRadius);
		chunk.clayxelMaterial.SetFloat("chunkSize", (float)this.chunkSize);
		chunk.clayxelMaterial.SetVector("chunkCenter",  chunk.center);
		chunk.clayxelMaterial.SetInt("solidHighlightId", -1);

		chunk.clayxelPickingMaterial.SetFloat("chunkSize", (float)this.chunkSize);
		chunk.clayxelPickingMaterial.SetVector("chunkCenter",  chunk.center);
		chunk.clayxelPickingMaterial.SetFloat("splatRadius",  this.splatRadius);
	}

	void scanRecursive(Transform trn){
		if(this.clayObjects.Count == 128){
			return;
		}

		ClayObject clayObj = trn.gameObject.GetComponent<ClayObject>();
		if(clayObj != null){
			if(clayObj.isValid() && trn.gameObject.activeSelf){
				this.clayObjects.Add(new WeakReference(clayObj));
				clayObj.transform.hasChanged = true;
				clayObj.cacheClayxelContainer();
			}
		}

		for(int i = 0; i < trn.childCount; ++i){
			this.scanRecursive(trn.GetChild(i));
		}
	}

	public void scanSolids(){
		this.clayObjects.Clear();

		this.scanRecursive(this.transform);
	}

	int getBufferCount(ComputeBuffer buffer){
		ComputeBuffer.CopyCount(buffer, this.countBuffer, 0);
		this.countBuffer.GetData(this.countBufferArray);
		int count = this.countBufferArray[0];

		return count;
	}

	#if DEBUGGRIDPOINTS
	void debugDisplayPoints(ClayxelChunk chunk, int gridSideCount){
		chunk.debugGridOutPointsBuffer.SetCounterValue(0);

		Clayxel.claycoreCompute.SetInt("debugGridSideCount", gridSideCount);
		
		Clayxel.claycoreCompute.SetBuffer((int)Kernels.debugDisplayGridPoints, "debugGridOutPoints", chunk.debugGridOutPointsBuffer);

		Clayxel.claycoreCompute.Dispatch((int)Kernels.debugDisplayGridPoints, gridSideCount, gridSideCount, gridSideCount);

		// ComputeBuffer.CopyCount(chunk.debugGridOutPointsBuffer, chunk.clayxelShaderArgs, 0);
	}
	#endif

	void computeChunkPoints(ClayxelChunk chunk){
		Clayxel.claycoreCompute.SetVector("chunkCenter", chunk.center);
		Clayxel.claycoreCompute.Dispatch((int)Kernels.compute0, 1, 1, 1);
		Clayxel.claycoreCompute.Dispatch((int)Kernels.compute2, 1, 1, 1);
		Clayxel.claycoreCompute.Dispatch((int)Kernels.cache4, this.numThreadsCompute4, this.numThreadsCompute4, this.numThreadsCompute4);
		Clayxel.claycoreCompute.Dispatch((int)Kernels.compute4, this.numThreadsCompute4, this.numThreadsCompute4, this.numThreadsCompute4);

		#if DEBUGGRIDPOINTS
			this.debugDisplayPoints(chunk, 64);
			return;
		#endif

		Clayxel.claycoreCompute.Dispatch((int)Kernels.cache6, this.numThreadsCompute6, this.numThreadsCompute6, this.numThreadsCompute6);
		Clayxel.claycoreCompute.Dispatch((int)Kernels.compute6, this.numThreadsCompute6, this.numThreadsCompute6, this.numThreadsCompute6);
	}

	void updateChunk(int chunkId){
		ClayxelChunk chunk = this.chunks[chunkId];
		
		this.computeChunkPoints(chunk);

		#if DEBUGGRIDPOINTS
			return;
		#endif

		// generate point cloud
		chunk.chunkOutPointsBuffer.SetCounterValue(0);

		// Clayxel.claycoreCompute.SetBuffer((int)Kernels.genPointCloud, "indirectDrawArgs", chunk.clayxelShaderArgs);
		Clayxel.claycoreCompute.SetBuffer((int)Kernels.genPointCloud, "chunkOutPoints", chunk.chunkOutPointsBuffer);
		Clayxel.claycoreCompute.Dispatch((int)Kernels.genPointCloud, this.numThreadsCompute6, this.numThreadsCompute6, this.numThreadsCompute6);

		// clear chunk
		Clayxel.claycoreCompute.Dispatch((int)Kernels.clearChunkData, this.numThreadsCompute6, this.numThreadsCompute6, this.numThreadsCompute6);

		// set material params
		chunk.clayxelMaterial.SetFloat("_Smoothness", this.materialSmoothness);
		chunk.clayxelMaterial.SetFloat("_Metallic", this.materialMetallic);
		chunk.clayxelMaterial.SetColor("_Emission", this.materialEmission);
		chunk.clayxelMaterial.SetFloat("normalOrientedSplat", this.normalOrientedSplat);
		chunk.clayxelMaterial.SetFloat("splatSizeMult", this.splatSizeMultiplier);
	}

	public int getNumClayObjects(){
		return  this.clayObjects.Count;
	}

	void updateSolids(){
		Matrix4x4 thisMatInv = this.transform.worldToLocalMatrix;

		for(int i = 0; i < this.clayObjects.Count; ++i){
			ClayObject clayObj = (ClayObject)this.clayObjects[i].Target;
			Matrix4x4 clayObjMat = thisMatInv * clayObj.transform.localToWorldMatrix;

			float blend = clayObj.blend;
			if(blend < 0.0f){
				blend = clayObj.blend - (this.splatRadius * 2.0f);
			}

			this.solidsPos[i] = (Vector3)clayObjMat.GetColumn(3);
			this.solidsRot[i] = clayObjMat.rotation;
			this.solidsScale[i] = clayObj.transform.localScale*0.5f;
			this.solidsBlend[i] = blend;
			this.solidsType[i] = clayObj.primitiveType;
			this.solidsColor[i] = new Vector3(clayObj.color.r, clayObj.color.g, clayObj.color.b);
			this.solidsAttrs[i] = clayObj.attrs;
		}

		if(this.clayObjects.Count == 0){
			return;
		}

		this.solidsPosBuffer.SetData(this.solidsPos);
		this.solidsRotBuffer.SetData(this.solidsRot);
		this.solidsScaleBuffer.SetData(this.solidsScale);
		this.solidsBlendBuffer.SetData(this.solidsBlend);
		this.solidsTypeBuffer.SetData(this.solidsType);
		this.solidsColorBuffer.SetData(this.solidsColor);
		this.solidsAttrsBuffer.SetData(this.solidsAttrs);
	}

	void logFPS(){
		this.deltaTime += (Time.unscaledDeltaTime - this.deltaTime) * 0.1f;
		float fps = 1.0f / this.deltaTime;
		Debug.Log(fps);
	}

	void switchComputeData(){
		Clayxel.lastUpdatedContainerId = this.GetInstanceID();

		int numKernels = Enum.GetNames(typeof(Kernels)).Length;
		for(int i = 0; i < numKernels; ++i){
			Clayxel.claycoreCompute.SetBuffer(i, "solidsPos", this.solidsPosBuffer);
			Clayxel.claycoreCompute.SetBuffer(i, "solidsRot", this.solidsRotBuffer);
			Clayxel.claycoreCompute.SetBuffer(i, "solidsScale", this.solidsScaleBuffer);
			Clayxel.claycoreCompute.SetBuffer(i, "solidsBlend", this.solidsBlendBuffer);
			Clayxel.claycoreCompute.SetBuffer(i, "solidsType", this.solidsTypeBuffer);
			Clayxel.claycoreCompute.SetBuffer(i, "solidsColor", this.solidsColorBuffer);
			Clayxel.claycoreCompute.SetBuffer(i, "solidsAttrs", this.solidsAttrsBuffer);
		}

		Clayxel.claycoreCompute.SetFloat("globalRoundCornerValue", this.splatRadius * 2.0f);
	}

	void updateChunksTransform(){

		Vector3 scale = this.transform.localScale;
		float splatScale = (scale.x + scale.y + scale.z) / 3.0f;

		// clayxels are computed at the center of the world, so we need to transform them back when this transform is updated
		for(int chunkIt = 0; chunkIt < this.numChunks; ++chunkIt){
			ClayxelChunk chunk = this.chunks[chunkIt];

			chunk.clayxelMaterial.SetMatrix("objectMatrix", this.transform.localToWorldMatrix);

			chunk.clayxelMaterial.SetFloat("splatRadius", this.splatRadius * splatScale);
			chunk.clayxelPickingMaterial.SetFloat("splatRadius",  this.splatRadius * splatScale);
		}
	}
	
	public void Update(){
		if(this.meshCached){
			return;
		}

		if(this.invalidated){
			return;
		}

		if(this.needsInit){
			this.init();
		}
		else{
			// if we're not initializing this grid, then inhibit updates if this transform is the trigger.
			// rigid transforms will be handled by the shader at render time
			if(this.transform.hasChanged){
				this.transform.hasChanged = false;
				this.needsUpdate = false;

				this.updateChunksTransform();
			}
		}

		if(this.forceUpdate){
			// if user is moving a clay object while also moving this container, then he'll need to use forceClayUpdate()
			this.needsUpdate = true;
			this.forceUpdate = false;
		}

		if(!this.needsUpdate){
			this.drawClayxels();
			return;
		}
		
		this.needsUpdate = false;
		
		if(this.solidsContainerNeedsUpdate){
			this.scanSolids();
			this.solidsContainerNeedsUpdate = false;
		}
		
		if(Clayxel.lastUpdatedContainerId != this.GetInstanceID()){
			this.switchComputeData();
		}
		
		this.updateSolids();

		Clayxel.claycoreCompute.SetInt("numSolids", this.clayObjects.Count);
		Clayxel.claycoreCompute.SetFloat("chunkSize", (float)this.chunkSize);

		for(int chunkIt = 0; chunkIt < this.numChunks; ++chunkIt){
			this.updateChunk(chunkIt);
		}

		this.drawClayxels();
	}

	public void drawClayxels(){
		if(this.needsInit){
			return;
		}

		// uncomment if full indirect
		// this.countBuffer.GetData(this.countBufferArray);// if I don't do at least one getdata of a buffer, everything goes SLOOOOOW, why?!?!?

		this.renderBounds.center = this.transform.position;
		this.renderBounds.size = this.boundsScale;

		for(int chunkIt = 0; chunkIt < this.numChunks; ++chunkIt){
			ClayxelChunk chunk = this.chunks[chunkIt];

			#if DEBUGGRIDPOINTS 
				int pnts = this.getBufferCount(chunk.debugGridOutPointsBuffer);
				Graphics.DrawProcedural(chunk.clayxelMaterial, 
					this.renderBounds,
					MeshTopology.Points, pnts, 1);

				return;
			#endif

			int numpoints = this.getBufferCount(chunk.chunkOutPointsBuffer) * 3;

			// Graphics.DrawProceduralIndirect(chunk.clayxelMaterial, 
			// 	this.renderBounds,
			// 	MeshTopology.Triangles, chunk.clayxelShaderArgs, 0,//numpoints * 3, 1,
			// 	null, null,
			// 	ShadowCastingMode.TwoSided, true, this.gameObject.layer);
			
			Graphics.DrawProcedural(chunk.clayxelMaterial, 
				this.renderBounds,
				MeshTopology.Triangles, numpoints, 1,
				null, null,
				ShadowCastingMode.TwoSided, true, this.gameObject.layer);
		}
	}

	public ClayObject addSolid(){
		GameObject clayObj = new GameObject("claySolid");
		clayObj.transform.parent = this.transform;

		ClayObject clayObjComp = clayObj.AddComponent<ClayObject>();
		clayObjComp.clayxelContainerRef = new WeakReference(this);
		clayObjComp.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);

		this.solidsContainerNeedsUpdate = true;

		return clayObjComp;
	}

	public ClayObject getClayObj(int id){
		return (ClayObject)this.clayObjects[id].Target;
	}

	public void solidsNeedUpdate(){
		this.solidsContainerNeedsUpdate = true;
	}

	public void generateMesh(){
		this.meshCached = true;

		if(this.gameObject.GetComponent<MeshFilter>() == null){
			this.gameObject.AddComponent<MeshFilter>();
		}
		
		MeshRenderer render = this.gameObject.GetComponent<MeshRenderer>();
		if(render == null){
			render = this.gameObject.AddComponent<MeshRenderer>();
			render.material = new Material(Shader.Find("Clayxel/ClayxelMeshShader"));
		}

		render.sharedMaterial.SetFloat("_Glossiness", this.materialSmoothness);
		render.sharedMaterial.SetFloat("_Metallic", this.materialMetallic);
		render.sharedMaterial.SetColor("_Emission", this.materialEmission);

		ComputeBuffer meshIndicesBuffer = new ComputeBuffer(this.chunkMaxOutPoints * 6, sizeof(float) * 3, ComputeBufferType.Counter);
		Clayxel.claycoreCompute.SetBuffer((int)Kernels.genMesh, "meshOutIndices", meshIndicesBuffer);

		ComputeBuffer meshVertsBuffer = new ComputeBuffer(this.chunkMaxOutPoints, sizeof(float) * 3, ComputeBufferType.Counter);
		Clayxel.claycoreCompute.SetBuffer((int)Kernels.genMesh, "meshOutPoints", meshVertsBuffer);

		ComputeBuffer meshColorsBuffer = new ComputeBuffer(this.chunkMaxOutPoints, sizeof(float) * 4);
		Clayxel.claycoreCompute.SetBuffer((int)Kernels.genMesh, "meshOutColors", meshColorsBuffer);

		List<Vector3> totalVertices = new List<Vector3>();
		List<int> totalIndices = new List<int>();
		List<Color> totalColors = new List<Color>();

		int totalNumVerts = 0;

		this.mesh = new Mesh();
		this.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

		this.switchComputeData();

		this.updateSolids();

		Clayxel.claycoreCompute.SetInt("numSolids", this.clayObjects.Count);
		Clayxel.claycoreCompute.SetFloat("chunkSize", (float)this.chunkSize);

		for(int chunkIt = 0; chunkIt < this.numChunks; ++chunkIt){
			ClayxelChunk chunk = this.chunks[chunkIt];

			meshIndicesBuffer.SetCounterValue(0);
			meshVertsBuffer.SetCounterValue(0);

			this.computeChunkPoints(chunk);

			Clayxel.claycoreCompute.SetInt("outMeshIndexOffset", totalNumVerts);
			Clayxel.claycoreCompute.Dispatch((int)Kernels.genMesh, 16, 16, 16);

			Clayxel.claycoreCompute.Dispatch((int)Kernels.clearChunkData, this.numThreadsCompute6, this.numThreadsCompute6, this.numThreadsCompute6);

			int numVerts = this.getBufferCount(meshVertsBuffer);
			int numQuads = this.getBufferCount(meshIndicesBuffer) * 3;

			totalNumVerts += numVerts;
			
			Vector3[] vertices = new Vector3[numVerts];
			meshVertsBuffer.GetData(vertices);

			int[] indices = new int[numQuads];
			meshIndicesBuffer.GetData(indices);

			Color[] colors = new Color[numVerts];
			meshColorsBuffer.GetData(colors);

			totalVertices.AddRange(vertices);
			totalIndices.AddRange(indices);
			totalColors.AddRange(colors);
		}

		mesh.vertices = totalVertices.ToArray();
		mesh.colors = totalColors.ToArray();
		mesh.triangles = totalIndices.ToArray();

		this.mesh.Optimize();
		NormalSolver.RecalculateNormals(this.mesh, 120.0f);
		this.gameObject.GetComponent<MeshFilter>().mesh = this.mesh;

		meshIndicesBuffer.Release();
		meshVertsBuffer.Release();
		meshColorsBuffer.Release();

		this.releaseBuffers();
		this.needsInit = false;
	}

	public bool hasCachedMesh(){
		return this.meshCached;
	}

	public void disableMesh(){
		this.meshCached = false;
		this.needsInit = true;

		if(this.gameObject.GetComponent<MeshRenderer>() != null){
			DestroyImmediate(this.gameObject.GetComponent<MeshRenderer>());
		}

		if(this.gameObject.GetComponent<MeshFilter>() != null){
			DestroyImmediate(this.gameObject.GetComponent<MeshFilter>());
		}
	}

	static public void reloadSolidsCatalogue(){
		Clayxel.solidsCatalogueLabels.Clear();
		Clayxel.solidsCatalogueParameters.Clear();

		int lastParsed = -1;
		try{
			TextAsset txt = (TextAsset)Resources.Load("claySDF", typeof(TextAsset));
			string content = txt.text;

			string numThreadsDef = "MAXTHREADS";
			Clayxel.maxThreads = (int)char.GetNumericValue(content[content.IndexOf(numThreadsDef) + numThreadsDef.Length + 1]);

			string[] lines = content.Split(new[]{ "\r\n", "\r", "\n" }, StringSplitOptions.None);
			for(int i = 0; i < lines.Length; ++i){
				string line = lines[i];
				if(line.Contains("label")){
					lastParsed += 1;
					string[] parameters = line.Split(new[]{"label:"}, StringSplitOptions.None)[1].Split(',');
					string label = parameters[0].Trim();
					
					Clayxel.solidsCatalogueLabels.Add(label);

					List<string[]> paramList = new List<string[]>();

					for(int paramIt = 1; paramIt < parameters.Length; ++paramIt){
						string param = parameters[paramIt];
						string[] attrs = param.Split(':');
						string paramId = attrs[0];
						string[] paramLabelValue = attrs[1].Split(' ');
						string paramLabel = paramLabelValue[1];
						string paramValue = paramLabelValue[2];

						paramList.Add(new string[]{paramId.Trim(), paramLabel.Trim(), paramValue.Trim()});
					}

					Clayxel.solidsCatalogueParameters.Add(paramList);
				}
			}
		}
		catch{
			Debug.Log("error trying to parse solid parameters in claySDF.txt, solid #" + lastParsed);
		}
	}

	public string[] getSolidsCatalogueLabels(){
		return Clayxel.solidsCatalogueLabels.ToArray();
	}

	public List<string[]> getSolidsCatalogueParameters(int solidId){
		return Clayxel.solidsCatalogueParameters[solidId];
	}

	public void updateSplatLook(){
		for(int chunkIt = 0; chunkIt < this.numChunks; ++chunkIt){
			ClayxelChunk chunk = this.chunks[chunkIt];
			chunk.clayxelMaterial.SetTexture("_MainTex", this.splatTexture);

			if(this.splatTexture != null){
				chunk.clayxelMaterial.EnableKeyword("SPLATTEXTURE_ON");
				chunk.clayxelMaterial.DisableKeyword("SPLATTEXTURE_OFF");
			}
			else{
				chunk.clayxelMaterial.DisableKeyword("SPLATTEXTURE_ON");
				chunk.clayxelMaterial.EnableKeyword("SPLATTEXTURE_OFF");
			}
		}
	}

	#if UNITY_EDITOR
	int pickingHighlight = -1;
	bool editingThisContainer = false;

	void OnValidate(){
		// called when editor value on this object is changed
		this.numChunks = 0;
	}

	void removeEditorEvents(){
		AssemblyReloadEvents.beforeAssemblyReload -= this.onBeforeAssemblyReload;

		EditorApplication.hierarchyChanged -= this.onSolidsHierarchyOrderChanged;

		UnityEditor.Selection.selectionChanged -= this.onSelectionChanged;

		UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= this.onSceneSaved;
		
		Undo.undoRedoPerformed -= this.onUndoPerformed;

		EditorApplication.update -= Clayxel.onUnityEditorUpdate;
	}

	void reinstallEditorEvents(){
		this.removeEditorEvents();

		AssemblyReloadEvents.beforeAssemblyReload += this.onBeforeAssemblyReload;

		EditorApplication.hierarchyChanged += this.onSolidsHierarchyOrderChanged;

		UnityEditor.Selection.selectionChanged += this.onSelectionChanged;

		UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += this.onSceneSaved;

		Undo.undoRedoPerformed += this.onUndoPerformed;

		EditorApplication.update -= Clayxel.onUnityEditorUpdate;
		EditorApplication.update += Clayxel.onUnityEditorUpdate;
	}

	static bool _appFocused = true;
	static void onUnityEditorUpdate(){
		if(!Clayxel._appFocused && UnityEditorInternal.InternalEditorUtility.isApplicationActive){
			Clayxel._appFocused = UnityEditorInternal.InternalEditorUtility.isApplicationActive;
			Clayxel.reloadAll();
		}
		else if (Clayxel._appFocused && !UnityEditorInternal.InternalEditorUtility.isApplicationActive){
			Clayxel._appFocused = UnityEditorInternal.InternalEditorUtility.isApplicationActive;
		}
	}

	void onBeforeAssemblyReload(){
		// called when this script recompiles

		if(Application.isPlaying){
			return;
		}

		this.releaseBuffers();
		Clayxel.releaseGlobalBuffers();

		Clayxel.globalDataNeedsInit = true;
		this.needsInit = true;
	}

	void onSceneSaved(UnityEngine.SceneManagement.Scene scene){
		// saving a scene will break some of the stored data, we need to reinit
		this.needsInit = true;
	}

	void onUndoPerformed(){
		if(Undo.GetCurrentGroupName() == "changed clayobject" ||
			Undo.GetCurrentGroupName() == "changed clayxel container"){
			this.needsUpdate = true;
		}
		else if(Undo.GetCurrentGroupName() == "changed clayxel grid"){
			this.init();
		}
		else if(Undo.GetCurrentGroupName() == "added clayxel solid"){
			this.needsUpdate = true;
		}
		else if(Undo.GetCurrentGroupName() == "Selection Change"){
			if(UnityEditor.Selection.Contains(this.gameObject)){
				this.init();
			}
			else{
				if(UnityEditor.Selection.gameObjects.Length > 0){
					ClayObject clayObj = UnityEditor.Selection.gameObjects[0].GetComponent<ClayObject>();
					if(clayObj != null){
						if(clayObj.getClayxelContainer() == this){
							this.needsUpdate = true;
						}
					}
				}
			}
		}
	}

	void onSolidsHierarchyOrderChanged(){
		if(this.meshCached){
			return;
		}

		if(this.invalidated){
			// scene is being cleared
			return;
		}

		this.solidsContainerNeedsUpdate = true;
		this.needsUpdate = true;
		this.onSelectionChanged();
		
		UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
		this.getSceneView().Repaint();
		#if DEBUG_CLAYXEL_REPAINT_WARN
		Debug.Log("onSolidsHierarchyOrderChanged!");
		#endif
	}

	void onSelectionChanged(){
		if(this.invalidated){
			return;
		}

		if(this.meshCached){
			return;
		}

		this.editingThisContainer = false;
		if(UnityEditor.Selection.Contains(this.gameObject)){
			// check if this container got selected
			this.editingThisContainer = true;
		}

		if(!this.editingThisContainer){
			// check if one of thye clayObjs in container has been selected
			for(int i = 0; i < this.clayObjects.Count; ++i){
				ClayObject clayObj = (ClayObject)this.clayObjects[i].Target;
				if(clayObj != null){
					if(UnityEditor.Selection.Contains(clayObj.gameObject)){
						this.editingThisContainer = true;
						return;
					}
				}
			}
		}

		if(Clayxel.lastUpdatedContainerId != this.GetInstanceID()){
			this.switchComputeData();
		}
	}

	static void setupSceneViewer(){
		if(Application.isPlaying){
			return;
		}

		SceneView sceneView = (SceneView)SceneView.sceneViews[0];
		if(sceneView == null){
			Debug.Log("failed to find a valid viewport, press reload again when you have a 3d view visible");
			return;
		}

		Camera camera = sceneView.camera;
		// camera.renderingPath = RenderingPath.Forward;//DeferredShading;

		ClaySceneViewer oldClaySceneView = camera.gameObject.GetComponent<ClaySceneViewer>();
		if(oldClaySceneView != null){
			SceneView.duringSceneGui -= oldClaySceneView.onSceneGUI;
			// oldClaySceneView.OnLostFocus -= oldClaySceneView.onLostFocus;
			ClaySceneViewer.globalInstanceRef = null;
			DestroyImmediate(oldClaySceneView);
		}

		ClaySceneViewer claySceneView = (ClaySceneViewer)camera.gameObject.AddComponent<ClaySceneViewer>();
		ClaySceneViewer.globalInstanceRef = new WeakReference(claySceneView);

		SceneView.duringSceneGui += claySceneView.onSceneGUI;
	}

	public void setPickingHighlight(int solidId){
		if(solidId != this.pickingHighlight){
			for(int chunkIt = 0; chunkIt < this.numChunks; ++chunkIt){
				ClayxelChunk chunk = this.chunks[chunkIt];
				chunk.clayxelMaterial.SetInt("solidHighlightId", solidId);
			}
		}

		this.pickingHighlight = solidId;
	}

	public void drawClayxelPicking(int clayxelId){
		if(this.needsInit){
			return;
		}

		for(int chunkIt = 0; chunkIt < this.numChunks; ++chunkIt){
			ClayxelChunk chunk = this.chunks[chunkIt];

			int numpoints = this.getBufferCount(chunk.chunkOutPointsBuffer) * 3;

			chunk.clayxelPickingMaterial.SetPass(0);
			chunk.clayxelPickingMaterial.SetMatrix("objectMatrix", this.transform.localToWorldMatrix);
			chunk.clayxelPickingMaterial.SetInt("clayxelId", clayxelId);
			
			Graphics.DrawProceduralNow(MeshTopology.Triangles, numpoints);
		}
	}

	void OnDrawGizmos(){
		if(Application.isPlaying){
			return;
		}

		if(!this.showContainer){
			return;
		}

		if(!this.editingThisContainer){
			return;
		}

		Gizmos.color = new Color(0.5f, 0.5f, 1.0f, 0.1f);
		Gizmos.matrix = this.transform.localToWorldMatrix;
		Gizmos.DrawWireCube(this.boundsCenter, this.boundsScale);

		// debug chunks
		// Vector3 boundsScale2 = new Vector3(this.chunkSize, this.chunkSize, this.chunkSize);
		// for(int i = 0; i < this.numChunks; ++i){
		// 	Gizmos.DrawWireCube(this.chunks[i].center, boundsScale2);
		// }
	}

	static public void reloadAll(){
		Clayxel.globalDataNeedsInit = true;

		Clayxel[] clayxelObjs = UnityEngine.Object.FindObjectsOfType<Clayxel>();
		for(int i = 0; i < clayxelObjs.Length; ++i){
			clayxelObjs[i].init();
		}
		
		UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
		((SceneView)SceneView.sceneViews[0]).Repaint();
	}

	public SceneView getSceneView(){
		return (SceneView)SceneView.sceneViews[0];
	}

	#endif
}