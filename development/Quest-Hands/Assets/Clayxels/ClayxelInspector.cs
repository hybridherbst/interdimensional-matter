#if UNITY_EDITOR // exclude from build

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(Clayxel))]
public class ClayxelInspector : Editor{
	public override void OnInspectorGUI(){
		Clayxel clayxel = (Clayxel)this.target;

		EditorGUILayout.LabelField("Clayxels, V0.46 beta");
		EditorGUILayout.LabelField("contained clayObjects: " + clayxel.getNumClayObjects());
		EditorGUILayout.LabelField("limit is 64 on free version");
		EditorGUILayout.Space();

		EditorGUI.BeginChangeCheck();
		int chunkSize = EditorGUILayout.IntField("resolution", clayxel.chunkSize);
		int numChunksX = EditorGUILayout.IntField("containerSizeX", clayxel.numChunksX);
		int numChunksY = EditorGUILayout.IntField("containerSizeY", clayxel.numChunksY);
		int numChunksZ = EditorGUILayout.IntField("containerSizeZ", clayxel.numChunksZ);
		
		if(EditorGUI.EndChangeCheck()){
			Undo.RecordObject(this.target, "changed clayxel grid"); 

			clayxel.chunkSize = chunkSize;
			clayxel.numChunksX = numChunksX;
			clayxel.numChunksY = numChunksY;
			clayxel.numChunksZ = numChunksZ;

			clayxel.init();
			clayxel.needsUpdate = true;
			UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
			clayxel.getSceneView().Repaint();

			#if DEBUG_CLAYXEL_REPAINT_WARN
			Debug.Log("ClayxelInspector 1!");
			#endif

			return;
		}

		bool showContainer = EditorGUILayout.Toggle("showContainer", clayxel.showContainer);

		EditorGUILayout.Space();

		EditorGUI.BeginChangeCheck();
		float materialSmoothness = EditorGUILayout.FloatField("Smoothness", clayxel.materialSmoothness);
		float materialMetallic = EditorGUILayout.FloatField("Metallic", clayxel.materialMetallic);
		Color materialEmission = EditorGUILayout.ColorField("Emission", clayxel.materialEmission);
		float splatSizeMultiplier = EditorGUILayout.FloatField("clayxelsSize", clayxel.splatSizeMultiplier);
		float normalOrientedSplat = EditorGUILayout.FloatField("clayxelsNormalOriented", clayxel.normalOrientedSplat);
		Texture2D splatTexture = (Texture2D)EditorGUILayout.ObjectField("clayxelsTexture", clayxel.splatTexture, typeof(Texture2D), false);

		if(EditorGUI.EndChangeCheck()){
			Undo.RecordObject(this.target, "changed clayxel container");

			clayxel.showContainer = showContainer;
			clayxel.materialSmoothness = materialSmoothness;
			clayxel.materialMetallic = materialMetallic;
			clayxel.materialEmission = materialEmission;
			clayxel.splatSizeMultiplier = splatSizeMultiplier;
			clayxel.normalOrientedSplat = normalOrientedSplat;
			clayxel.splatTexture = splatTexture;

			if(clayxel.normalOrientedSplat < 0.0f){
				clayxel.normalOrientedSplat = 0.0f;
			}
			else if(clayxel.normalOrientedSplat > 1.0f){
				clayxel.normalOrientedSplat = 1.0f;
			}

			clayxel.updateSplatLook();
			
			clayxel.needsUpdate = true;
			UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
			clayxel.getSceneView().Repaint();
			#if DEBUG_CLAYXEL_REPAINT_WARN
			Debug.Log("ClayxelInspector 2!");
			#endif

			return;
		}

		EditorGUILayout.Space();

		if(GUILayout.Button("reload all")){
			Clayxel.reloadAll();
		}

		if(GUILayout.Button("pick solid (p)")){
			ClaySceneViewer.getGlobalInstance().startPicking();
		}

		if(GUILayout.Button("add solid")){
			ClayObject clayObj = ((Clayxel)this.target).addSolid();

			Undo.RegisterCreatedObjectUndo(clayObj.gameObject, "added clayxel solid");
			UnityEditor.Selection.objects = new GameObject[]{clayObj.gameObject};

			clayxel.needsUpdate = true;
			UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
			clayxel.getSceneView().Repaint();

			return;
		}

		if(!clayxel.hasCachedMesh()){
			if(GUILayout.Button("freeze to mesh")){
				clayxel.generateMesh();
			}
		}
		else{
			if(GUILayout.Button("defrost clayxels")){
				clayxel.disableMesh();
				UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
				clayxel.getSceneView().Repaint();
			}
		}
	}
}

[CustomEditor(typeof(ClayObject)), CanEditMultipleObjects]
public class ClayObjectInspector : Editor{
	
	public override void OnInspectorGUI(){
		ClayObject clayObj = (ClayObject)this.targets[0];

		EditorGUI.BeginChangeCheck();

		float blend = EditorGUILayout.FloatField("blend", clayObj.blend);

		Color color = EditorGUILayout.ColorField("color", clayObj.color);
		
		Clayxel clayxel = clayObj.getClayxelContainer();
 		int primitiveType = EditorGUILayout.Popup("solidType", clayObj.primitiveType, clayxel.getSolidsCatalogueLabels());

 		Dictionary<string, float> paramValues = new Dictionary<string, float>();
 		paramValues["x"] = clayObj.attrs.x;
 		paramValues["y"] = clayObj.attrs.y;
 		paramValues["z"] = clayObj.attrs.z;
 		paramValues["w"] = clayObj.attrs.w;

 		List<string[]> parameters = clayxel.getSolidsCatalogueParameters(primitiveType);
 		List<string> wMaskLabels = new List<string>();
 		for(int paramIt = 0; paramIt < parameters.Count; ++paramIt){
 			string[] parameterValues = parameters[paramIt];
 			string attr = parameterValues[0];
 			string label = parameterValues[1];
 			string defaultValue = parameterValues[2];

 			if(primitiveType != clayObj.primitiveType){
 				// reset to default params when changing primitive type
 				paramValues[attr] = float.Parse(defaultValue);
 			}
 			
 			if(attr.StartsWith("w")){
 				wMaskLabels.Add(label);
 			}
 			else{
 				paramValues[attr] = EditorGUILayout.FloatField(label, paramValues[attr]);
 			}
 		}

 		if(wMaskLabels.Count > 0){
 			paramValues["w"] = (float)EditorGUILayout.MaskField("options", (int)clayObj.attrs.w, wMaskLabels.ToArray());
 		}

 		if(EditorGUI.EndChangeCheck()){
 			Undo.RecordObjects(this.targets, "changed clayobject");

 			for(int i = 1; i < this.targets.Length; ++i){
 				bool somethingChanged = false;
 				ClayObject currentClayObj = (ClayObject)this.targets[i];

 				if(clayObj.blend != blend){
 					currentClayObj.blend = blend;
 					somethingChanged = true;
 				}

 				if(clayObj.color != color){
 					currentClayObj.color = color;
 					somethingChanged = true;
 				}

 				if(clayObj.primitiveType != primitiveType){
 					currentClayObj.primitiveType = primitiveType;
 					somethingChanged = true;
 				}

 				if(clayObj.attrs.x != paramValues["x"]){
 					currentClayObj.attrs.x = paramValues["x"];
 					somethingChanged = true;
 				}

 				if(clayObj.attrs.y != paramValues["y"]){
 					currentClayObj.attrs.y = paramValues["y"];
 					somethingChanged = true;
 				}

 				if(clayObj.attrs.z != paramValues["z"]){
 					currentClayObj.attrs.z = paramValues["z"];
 					somethingChanged = true;
 				}

 				if(clayObj.attrs.w != paramValues["w"]){
 					currentClayObj.attrs.w = paramValues["w"];
 					somethingChanged = true;
 				}

 				if(somethingChanged){
 					currentClayObj.getClayxelContainer().needsUpdate = true;
 				}
			}

 			clayObj.blend = blend;
 			clayObj.color = color;
 			clayObj.primitiveType = primitiveType;
 			clayObj.attrs.x = paramValues["x"];
 			clayObj.attrs.y = paramValues["y"];
 			clayObj.attrs.z = paramValues["z"];
 			clayObj.attrs.w = paramValues["w"];
 			
 			clayxel.needsUpdate = true;
 			UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
 			clayxel.getSceneView().Repaint();
 			#if DEBUG_CLAYXEL_REPAINT_WARN
 			Debug.Log("editor update");
 			#endif
		}
	}
}

#endif // exclude from build