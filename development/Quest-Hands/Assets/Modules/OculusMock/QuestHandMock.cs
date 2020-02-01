using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestHandMock : MonoBehaviour
{
    public QuestHandMockMap map;
    public OVRTouchSample.Hand hand;
    OVRSkeleton skeleton;
    public IList<OVRBone> Bones {
        get {
#if !UNITY_EDITOR
            return skeleton.Bones;
#else
            if(mockBones == null) {
                ConstructBones();
            }
            return mockBones;
#endif
        }
    }

    List<OVRBone> mockBones;
    public List<MockOVRBone> _mockBones; 
    
    [System.Serializable]
    public class MockOVRBone
    {
        public OVRSkeleton.BoneId Id;
        public short ParentBoneIndex;
        public Transform Transform;

        public MockOVRBone(OVRSkeleton.BoneId id, short parentBoneIndex, Transform trans)
        {
            Id = id;
            ParentBoneIndex = parentBoneIndex;
            Transform = trans;
        }
    }

    private void Awake() {
        skeleton = GetComponent<OVRSkeleton>();
    }

    void ConstructBones() {
        mockBones = _mockBones.Select(x => new OVRBone(x.Id, x.ParentBoneIndex, x.Transform)).ToList();
    }

    [ContextMenu("Construct Mock Bones")]
    void ConstructMockBones() {
        // need to assemble full structure for hand mock!
        _mockBones = new List<MockOVRBone>();
        foreach(OVRSkeleton.BoneId e in System.Enum.GetValues(typeof(OVRSkeleton.BoneId))) {
            var t = map.FindTransform(e, hand);
            var i = t && t.parent ? map.FindBoneId(t.parent, hand) : 0;
            _mockBones.Add(new MockOVRBone(e, (short) i, t));
        }
    }
}
