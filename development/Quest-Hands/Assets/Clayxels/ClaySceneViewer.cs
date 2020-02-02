#if UNITY_EDITOR // exclude from build

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// This class is used for in-editor only clayObjects picking
// 

[ExecuteInEditMode]
public class ClaySceneViewer : MonoBehaviour{
	static public WeakReference globalInstanceRef = null;

	public static ClaySceneViewer getGlobalInstance(){
		if(ClaySceneViewer.globalInstanceRef == null){
			ClaySceneViewer globalInst = ((SceneView)SceneView.sceneViews[0]).camera.GetComponent<ClaySceneViewer>();
			ClaySceneViewer.globalInstanceRef = new WeakReference(globalInst);
		}

		return (ClaySceneViewer)ClaySceneViewer.globalInstanceRef.Target;
	}
	
	int mousePosX = 0;
	int mousePosY = 0;
	RenderTexture pickingRenderTexture = null;
	Texture2D pickingTextureResult;
	Rect rectReadPicture;
	bool pickingEnabled = false;
	Clayxel[] clayxels = new Clayxel[0]{};
	GameObject pickedClayObj = null;
	int pickedSolidId = -1;
	int pickedClayxelId = -1;
	bool wasShiftPressed = false;

	void Awake(){
	}

	void OnDestroy(){
		RenderTexture.ReleaseTemporary(this.pickingRenderTexture);
		this.pickingRenderTexture = null;
	}

	void getSolidIdUnderMouseCursor(int viewWidth, int viewHeight, out int pickedSolidId, out int pickedClayxelId){
		pickedSolidId = -1;
		pickedClayxelId = -1;
		if(this.mousePosX > -1 && this.mousePosX < viewWidth && this.mousePosY > -1 && this.mousePosY < viewHeight){
			#if UNITY_EDITOR_OSX
				// handle retina and flipped vertical coords
				this.rectReadPicture.Set(this.mousePosX * 2, viewHeight - (this.mousePosY * 2), 1, 1);
			#else
				this.rectReadPicture.Set(this.mousePosX, this.mousePosY, 1, 1);
			#endif

			this.pickingTextureResult.ReadPixels(this.rectReadPicture, 0, 0);
			this.pickingTextureResult.Apply();
			
			Color32 pickCol = this.pickingTextureResult.GetPixel(0, 0);
			
			int pickId = pickCol.r + pickCol.g * 256 + pickCol.b * 256 * 256;
	  		pickedSolidId = pickId - 1;
	  		pickedClayxelId = pickCol.a - 1;
			
	  		if(pickedSolidId > 256){
	  			pickedSolidId = -1;
	  			pickedClayxelId = -1;
	  		}
		}
	}

	void OnRenderImage(RenderTexture source, RenderTexture destination){
		if(!this.pickingEnabled){
			Graphics.Blit(source, destination);
			return;
		}

		if(this.clayxels.Length == 0){
			Graphics.Blit(source, destination);
			return;
		}

		if(this.pickingRenderTexture == null){
			int width = source.width;
			int height = source.height;

			this.pickingRenderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
			this.pickingTextureResult = new Texture2D(1, 1, TextureFormat.ARGB32, false);
		}
		
		Graphics.SetRenderTarget(this.pickingRenderTexture.colorBuffer, source.depthBuffer);

		for(int i = 0; i < this.clayxels.Length; ++i){
			Clayxel clayxel = this.clayxels[i];
			clayxel.drawClayxelPicking(i);
		}

		this.getSolidIdUnderMouseCursor(source.width, source.height, out this.pickedSolidId, out this.pickedClayxelId);
		
		for(int i = 0; i < this.clayxels.Length; ++i){
			Clayxel clayxel = this.clayxels[i];

			if(i == this.pickedClayxelId){
				clayxel.setPickingHighlight(this.pickedSolidId);
			}
			else{
				clayxel.setPickingHighlight(-1);
			}
		}

		Graphics.Blit(source, destination);

		((SceneView)SceneView.sceneViews[0]).Repaint();
	}

	void clearPicking(){
		for(int i = 0; i < this.clayxels.Length; ++i){
			Clayxel clayxel = this.clayxels[i];
			clayxel.setPickingHighlight(-1);
		}
		
		this.clayxels = new Clayxel[0]{};
		this.pickingEnabled = false;
		this.pickedSolidId = -1;
		this.pickedClayxelId = -1;
	}

	void pickingDone(){
		if(this.pickedClayxelId != -1 && this.pickedSolidId != -1 && this.pickedSolidId < 255){
			ClayObject clayObj = this.clayxels[this.pickedClayxelId].getClayObj(this.pickedSolidId);
			this.pickedClayObj = clayObj.gameObject;
		}

		this.clearPicking();
	}

	public void onSceneGUI(SceneView sceneView){
		Event ev = Event.current;
		
		if(ev.isKey){
			if(ev.keyCode == KeyCode.P){
				if(!this.pickingEnabled){
					this.startPicking();
				}
			}
			else if(ev.keyCode == KeyCode.R && ev.control){
				Clayxel.reloadAll();
			}
		}
		else if(ev.type == EventType.MouseMove){
   		this.mousePosX = (int)ev.mousePosition.x;
   		this.mousePosY = (int)ev.mousePosition.y;

   		// if we're here, it means selection of the pickedClayObj succeeded, clear it
   		this.pickedClayObj = null;
   	}
   	else if(ev.type == EventType.MouseDown && !ev.alt){
   		if(this.pickingEnabled){
   			ev.Use();

   			this.pickingDone();

   			this.wasShiftPressed = ev.shift;
   		}
   	}
   	else if(ev.type == EventType.MouseLeaveWindow){
   		if(this.pickingEnabled){
   			this.clearPicking();
   		}
   	}
   	// else if(ev.type == EventType.MouseEnterWindow){
   	// 	sceneView.Focus(); // make sure we can pick up ev.keyCode events
   	// }

   	if(this.pickedClayObj != null){
   		// we need to keep selecting the pickedClayObj untill the next mousemove event, or this selection will get overridden 

   		if(this.wasShiftPressed){
   			List<UnityEngine.Object> sel = new List<UnityEngine.Object>();
   			for(int i = 0; i < UnityEditor.Selection.objects.Length; ++i){
   				sel.Add(UnityEditor.Selection.objects[i]);
   			}
   			sel.Add(this.pickedClayObj.gameObject);
   			UnityEditor.Selection.objects = sel.ToArray();
   		}
   		else{
   			UnityEditor.Selection.objects = new GameObject[]{this.pickedClayObj.gameObject};
   		}

   		// don't clear pickedClayObj yet
   	}
	}

	public void startPicking(){
		this.pickingEnabled = true;
		this.clayxels = UnityEngine.Object.FindObjectsOfType<Clayxel>();
	}
}

#endif
