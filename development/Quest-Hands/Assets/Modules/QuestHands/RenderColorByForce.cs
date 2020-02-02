using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderColorByForce : MonoBehaviour
{
    public Renderer[] rs;
    public Gradient forceGradient;

    MaterialPropertyBlock materialProperty;

    HandDebugInfo info;

    // Update is called once per frame
    void Update()
    {
        if(!info) info = GetComponent<HandDebugInfo>();
        if(materialProperty == null) materialProperty = new MaterialPropertyBlock();
        materialProperty.SetColor("_BaseColor", forceGradient.Evaluate(info.force));
        for(int i = 0; i < rs.Length; i++)
            rs[i].SetPropertyBlock(materialProperty);
    }
}
