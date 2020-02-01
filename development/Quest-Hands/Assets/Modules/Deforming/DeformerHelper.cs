using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeformerHelper : MonoBehaviour
{
    public Transform deformerParent;

    [ContextMenu("Deform Now")]
    void Start()
    {
        var defs = deformerParent.GetComponentsInChildren<Deform.Deformer>();
        var def = GetComponent<Deform.Deformable>();
        foreach(var d in defs)
            def.DeformerElements.Add(new Deform.DeformerElement(d, true));

        def.enabled = true;
    }

    [ContextMenu("Reset Now")]
    void Reset() {
        var def = GetComponent<Deform.Deformable>();
        def.DeformerElements.Clear();
        def.enabled = false;
    }
}
