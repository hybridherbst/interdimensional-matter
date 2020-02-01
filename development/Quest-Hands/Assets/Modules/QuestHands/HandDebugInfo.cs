using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class HandDebugInfo : MonoBehaviour
{
    public Text fingerBend;
    OVRSkeleton skeleton;

    // Start is called before the first frame update
    void Start()
    {
        skeleton = GetComponent<AttachToFinger>().skeleton;
    }

    OVRSkeleton.BoneId[] tips = new OVRSkeleton.BoneId[] {
        OVRSkeleton.BoneId.Hand_Thumb2,
        OVRSkeleton.BoneId.Hand_Index2,
        OVRSkeleton.BoneId.Hand_Middle2,
        OVRSkeleton.BoneId.Hand_Ring2,
        OVRSkeleton.BoneId.Hand_Pinky2
    };

    // Update is called once per frame
    void Update()
    {
        var str = "";

        foreach(var t in tips) {
            var b = skeleton.Bones.FirstOrDefault(x => x.Id == t);
            if(b != null)
                str += b.Id.ToString() + ": " + b.Transform.localEulerAngles + "\n";
        }

        fingerBend.text = str;
    }
}
