using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class HandDebugInfo : MonoBehaviour
{
    public Text fingerBend;
    public QuestHandMock skeleton;

    // Start is called before the first frame update
    void Start()
    {
        // skeleton = GetComponentInChildren<AttachToFinger>().skeleton;
        midTransforms = new List<OVRBone>();
        foreach(var t in mids) {
            var b = skeleton.Bones.FirstOrDefault(x => x.Id == t);
            if(b != null)
                midTransforms.Add(b);
        }
        
        tipTransforms = new List<OVRBone>();
        foreach(var t in tips) {
            var b = skeleton.Bones.FirstOrDefault(x => x.Id == t);
            if(b != null)
                tipTransforms.Add(b);
        }

        rootTransforms = new List<OVRBone>();
        foreach(var t in roots) {
            var b = skeleton.Bones.FirstOrDefault(x => x.Id == t);
            if(b != null)
                rootTransforms.Add(b);
        }

        wristTransform = skeleton.Bones.FirstOrDefault(x => x.Id == OVRSkeleton.BoneId.Hand_WristRoot).Transform;
    }

    OVRSkeleton.BoneId[] tips = new OVRSkeleton.BoneId[] {
        OVRSkeleton.BoneId.Hand_ThumbTip,
        OVRSkeleton.BoneId.Hand_IndexTip,
        OVRSkeleton.BoneId.Hand_MiddleTip,
        OVRSkeleton.BoneId.Hand_RingTip,
        OVRSkeleton.BoneId.Hand_PinkyTip
    };

    OVRSkeleton.BoneId[] mids = new OVRSkeleton.BoneId[] {
        OVRSkeleton.BoneId.Hand_Thumb2,
        OVRSkeleton.BoneId.Hand_Index2,
        OVRSkeleton.BoneId.Hand_Middle2,
        OVRSkeleton.BoneId.Hand_Ring2,
        OVRSkeleton.BoneId.Hand_Pinky2
    };

    OVRSkeleton.BoneId[] roots = new OVRSkeleton.BoneId[] {
        OVRSkeleton.BoneId.Hand_Thumb1,
        OVRSkeleton.BoneId.Hand_Pinky1
    };



    List<OVRBone> midTransforms;
    List<OVRBone> tipTransforms;
    List<OVRBone> rootTransforms;

    public Transform wristTransform;

    public Vector3 averageRootPos;
    public Vector3 averageTipPos;
    public Vector3 handDirection => (averageTipPos - averageRootPos).normalized;
    public float averageBendiness = 0f;

    public float force {
        get {
            // below 30: 0
            // 30 to 40: ramp 0..1
            // above 40: 1
            return Mathf.Clamp01(Remap(averageBendiness, 30, 40, 0, 1));
        }
    }

    float Remap(float val, float srcMin, float srcMax, float dstMin, float dstMax) {
        return (val - srcMin) / (srcMax - srcMin) * (dstMax - dstMin) + dstMin;
    }

    // Update is called once per frame
    void Update()
    {
        var str = "";
        averageBendiness = 0f;
        foreach(var b in midTransforms) {
            str += b.Id.ToString() + ": " + b.Transform.localEulerAngles + "\n";
            var val = b.Transform.localEulerAngles.z;
            if(val > 180) val -= 360;
            
            averageBendiness += val;
        }

        averageRootPos = Vector3.zero;
        foreach(var b in rootTransforms) {
            averageRootPos += b.Transform.position;
        }
        averageRootPos /= rootTransforms.Count;
        averageTipPos = Vector3.zero;
        foreach(var b in tipTransforms) {
            averageTipPos += b.Transform.position;
        }
        averageTipPos /= tipTransforms.Count;

        Debug.DrawLine(averageRootPos, averageTipPos, Color.black);

        averageBendiness /= midTransforms.Count;
        
        if(Application.isEditor)
            averageBendiness = -averageBendiness; // FOR MOCK HAND ONLY, OCULUS CUSTOM HAND RIG HAS REVERSED COORDINATES FOR SOME REASON

        str += "Avg: " + averageBendiness;

        fingerBend.text = str;
    }
}
