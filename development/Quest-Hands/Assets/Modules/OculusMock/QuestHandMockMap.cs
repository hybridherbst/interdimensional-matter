using System;
using System.Collections;
using System.Collections.Generic;
using OVRTouchSample;
using UnityEngine;

[CreateAssetMenu]
public class QuestHandMockMap : ScriptableObject
{
    public List<MockMap> map = new List<MockMap>();
    [System.Serializable]
    public class MockMap {
        public OVRSkeleton.BoneId id;
        public string match1, match2;

        public bool IsValid => !string.IsNullOrEmpty(match1);
    }

    [ContextMenu("Construct Mock Bones")]
    void ConstructMockBones() {
        // need to assemble full structure for hand mock!
        map = new List<MockMap>();
        foreach(OVRSkeleton.BoneId e in System.Enum.GetValues(typeof(OVRSkeleton.BoneId))) {
            map.Add(new MockMap { id = e });
        }
    }

    internal Transform FindTransform(OVRSkeleton.BoneId e, Hand hand)
    {
        var mapEntry = map.Find(x => x.id == e);

        var ts = hand.GetComponentsInChildren<Transform>();
        foreach(var t in ts) {
            if(mapEntry.IsValid && t.name.Contains(mapEntry.match1) && t.name.Contains(mapEntry.match2))
                return t;
        }

        return null;
    }

    internal OVRSkeleton.BoneId FindBoneId(Transform t, Hand hand)
    {
        if(!t || !hand) return OVRSkeleton.BoneId.Invalid;

        foreach(var mapEntry in map) {
            if(mapEntry.IsValid && t.name.Contains(mapEntry.match1) && t.name.Contains(mapEntry.match2))
                return mapEntry.id;
        }
        return OVRSkeleton.BoneId.Invalid;
    }
}
