using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeformerHelper : MonoBehaviour
{
    public Transform deformerParent;
    public string deformerParentTag = "Hands";

    [ContextMenu("Deform Now")]
    private void Awake() {
        // Debug.Log("Tag: " + deformerParentTag);
        if(!deformerParent)
            deformerParent = GameObject.FindWithTag(deformerParentTag).transform;

        var defs = deformerParent.GetComponentsInChildren<Deform.Deformer>();
        var def = GetComponent<Deform.Deformable>();
        foreach(var d in defs)
            def.DeformerElements.Add(new Deform.DeformerElement(d, true));

        def.enabled = true;
    }

    private void OnDisable() {
        // Reset();
    }

    [ContextMenu("Reset Now")]
    void Reset() {
        var def = GetComponent<Deform.Deformable>();
        def.DeformerElements.Clear();
        def.enabled = false;
    }
}
