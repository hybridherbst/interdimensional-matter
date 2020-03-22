using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.Oculus;
using UnityEngine.Rendering.Universal;

public class EnableFFR : MonoBehaviour
{
    public OVRPlugin.FixedFoveatedRenderingLevel ffrLevel = OVRPlugin.FixedFoveatedRenderingLevel.HighTop;
    void Start()
    {
        // Unity.XR.Oculus.Utils.SetFoveationLevel(null);
        OVRPlugin.fixedFoveatedRenderingLevel = ffrLevel;

        StartCoroutine(_LogLog());
    }

    IEnumerator _LogLog()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            LogCamState();
        }
    }

    [ContextMenu("Log Cam State")]
    void LogCamState() {
        // Debug.Log(UnityEngine.XR.XRSettings.stereoRenderingMode);
        // Debug.Log("can use single pass: " + UniversalRenderPipeline.CanXRSDKUseSinglePass(Camera.main));
    }
}
