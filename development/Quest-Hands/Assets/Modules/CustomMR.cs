using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomMR : MonoBehaviour
{
	bool inited = false;

    // Start is called before the first frame update
    void Initialize()
	{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || OVR_ANDROID_MRC
		if (inited)
			return;

#if OVR_ANDROID_MRC
		if (!OVRPlugin.Media.GetInitialized())
			return;
#else
		if (!OVRPlugin.IsMixedRealityInitialized())
			return;
#endif

		OVRPlugin.ResetDefaultExternalCamera();
		Debug.LogFormat("GetExternalCameraCount before adding manual external camera {0}", OVRPlugin.GetExternalCameraCount());
		// UpdateDefaultExternalCamera();
		Debug.LogFormat("GetExternalCameraCount after adding manual external camera {0}", OVRPlugin.GetExternalCameraCount());

		// obtain default FOV
		{
			OVRPlugin.CameraIntrinsics cameraIntrinsics;
			OVRPlugin.CameraExtrinsics cameraExtrinsics;
			OVRPlugin.Posef calibrationRawPose;
			OVRPlugin.GetMixedRealityCameraInfo(0, out cameraExtrinsics, out cameraIntrinsics);//, out calibrationRawPose);
		}

		inited = true;
#endif
	}


	void Update () {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || OVR_ANDROID_MRC
		if (!inited)
		{
			Initialize();
			return;
		}

#if OVR_ANDROID_MRC
		if (!OVRPlugin.Media.GetInitialized())
		{
			return;
		}
#else
		if (!OVRPlugin.IsMixedRealityInitialized())
		{
			return;
		}
#endif

        // UpdateDefaultExternalCamera();
        // OVRPlugin.OverrideExternalCameraFov(0, false, new OVRPlugin.Fovf());
        // OVRPlugin.OverrideExternalCameraStaticPose(0, false, OVRPlugin.Posef.identity);
		
#endif
    }

    public void SetMRC(bool isOn) {  
#if !OVR_ANDROID_MRC

		// On Quest, we enable MRC automatically through the configuration
		if (OVRManager.instance.enableMixedReality != isOn)
		{
			OVRManager.instance.enableMixedReality = isOn;
		}

        if(isOn) {
            StartCoroutine(_SetMaterialProps());
        }
#endif
    }

    IEnumerator _SetMaterialProps() {
        while(true)
        {
            yield return new WaitForSeconds(0.2f);
            var g = GameObject.Find("OculusMRC_CameraFrame");
            if(g) {

                var r = g.GetComponent<Renderer>();
                r.sharedMaterial.SetFloat("_Visible", 0.5f);
                break;
            }
        }
    }

    public void SetExternal(bool isExternal) {
        OVRManager.instance.compositionMethod = isExternal ? OVRManager.CompositionMethod.External : OVRManager.CompositionMethod.Direct;
    }
}