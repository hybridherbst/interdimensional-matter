using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.Oculus;

public class EnableFFR : MonoBehaviour
{
    public OVRPlugin.FixedFoveatedRenderingLevel ffrLevel = OVRPlugin.FixedFoveatedRenderingLevel.HighTop;
    void Start()
    {
        // Unity.XR.Oculus.Utils.SetFoveationLevel(null);
        OVRPlugin.fixedFoveatedRenderingLevel = ffrLevel;
    }
}
