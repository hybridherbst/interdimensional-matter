using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-69)]
public class AttachToFinger : MonoBehaviour
{
    public QuestHandMock skeleton;
    public OVRSkeleton.BoneId boneId;
    OVRBone bone;

    private bool debug = false;

    private void Start() {
        StartCoroutine(_Initialize());    
    }

    IEnumerator _Initialize() {
        yield return null;
        
        while(skeleton.Bones == null) {
            yield return null;
        }
        if(debug) Debug.Log("has bones");

        bone = null;
        while(bone == null) {
            bone = skeleton.Bones.FirstOrDefault(x => x.Id == boneId);
            yield return null;
        }
        if(debug) Debug.Log("Found Bone!");

        while(!bone.Transform)
            yield return null;
        if(debug) Debug.Log("Found Transform!");

        transform.SetParent(bone.Transform, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}
