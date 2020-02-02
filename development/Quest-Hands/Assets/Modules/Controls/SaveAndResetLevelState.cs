using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveAndResetLevelState : MonoBehaviour
{
    public struct State {
        public Vector3 localPos;
        public Quaternion localRot;
        public Vector3 localScale;
    }

    Dictionary<Transform, State> states = new Dictionary<Transform, State>();

    private void OnEnable() {
        if(states == null || states.Count == 0) {
            var ts = GetComponentsInChildren<Transform>();
            foreach(var t in ts) {
                states.Add(t, new State { localPos = t.localPosition, localRot = t.localRotation, localScale = t.localScale});
            }
        }
    }
    
    private void OnDisable() {
        foreach(var t in states) {
            var tt = t.Key;
            tt.localPosition = t.Value.localPos;
            tt.localRotation = t.Value.localRot;
            tt.localScale = t.Value.localScale;
        }    
    }
}
