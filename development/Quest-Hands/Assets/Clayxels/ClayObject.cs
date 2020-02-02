using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum ClayObjectType{
	cube,
	sphere,
	roundCube,
	cylinder,
	cone,
	torus,
	hexagon
};

[ExecuteInEditMode]
public class ClayObject : MonoBehaviour{
	public float blend = 0.0f;
	public Color color;
	public Vector4 attrs = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
	public WeakReference clayxelContainerRef = null;
	public int primitiveType = 0;

	bool invalidated = false;
	Color gizmoColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);
	
	void Awake(){
		this.cacheClayxelContainer();
	}

	void Update(){
		if(this.transform.hasChanged){
			this.transform.hasChanged = false;
			this.getClayxelContainer().needsUpdate = true;
		}
	}
	
	void OnDestroy(){
		this.invalidated = true;
		
		Clayxel clayxel = this.getClayxelContainer();
		if(clayxel != null){
			clayxel.solidsNeedUpdate();
		}
	}

	#if UNITY_EDITOR
	void OnDrawGizmos(){
		if(this.blend < 0.0f || // negative shape?
			(((int)this.attrs.w >> 0)&1) == 1){// painter?

			if(UnityEditor.Selection.Contains(this.gameObject)){// if selected draw wire cage
				Gizmos.color = this.gizmoColor;
				Gizmos.matrix = this.transform.localToWorldMatrix;

				if(this.primitiveType == 1){
					Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
				}
				else{
					Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
				}
			}
		}
	}
	#endif

	public bool isValid(){
		return !this.invalidated;
	}

	public Clayxel getClayxelContainer(){
		if(this.clayxelContainerRef != null){
			return (Clayxel)this.clayxelContainerRef.Target;
		}

		this.cacheClayxelContainer();

		return (Clayxel)this.clayxelContainerRef.Target;
	}

	public void cacheClayxelContainer(){
		this.clayxelContainerRef = null;
		GameObject parent = this.transform.parent.gameObject;

		Clayxel clayxel = null;
		for(int i = 0; i < 100; ++i){
			clayxel = parent.GetComponent<Clayxel>();
			if(clayxel != null){
				break;
			}
			else{
				parent = parent.transform.parent.gameObject;
			}
		}

		if(clayxel == null){
			Debug.Log("failed to find parent clayxel container");
		}
		else{
			this.clayxelContainerRef = new WeakReference(clayxel);
			clayxel.solidsNeedUpdate();
		}
	}

	public void setPrimitiveType(int primType){
		this.primitiveType = primType;
	}

	public Color getColor(){
		return this.color;
	}
}
