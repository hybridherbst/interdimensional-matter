using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteAlways]
public class DistanceGradient : MonoBehaviour
{
    public string uniformPrefix = "";
    public Transform[] pointsOfInterest;
    Vector4[] data = new Vector4[20];

    // Start is called before the first frame update
    void Start()
    {
        UpdatePOIs();
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePOIs();
    }

    void UpdatePOIs() {
        if (pointsOfInterest == null) return;

        Shader.SetGlobalInt(uniformPrefix + "_DistanceGradientCentersLength", pointsOfInterest.Length);

        //var arr = pointsOfInterest.Where(x => x).Select(x => new Vector4(x.position.x, x.position.y, x.position.z, x.lossyScale.x * 0.5f)).ToArray();
        for(int i = 0; i < pointsOfInterest.Length; i++)
            data[i] = new Vector4(pointsOfInterest[i].position.x, pointsOfInterest[i].position.y, pointsOfInterest[i].position.z, pointsOfInterest[i].localScale.x * 0.5f);

        Shader.SetGlobalVectorArray(uniformPrefix + "_DistanceGradientCenters", data);
    }

    void OnDrawGizmosSelected() {
        foreach(var t in pointsOfInterest) {
            Gizmos.DrawWireSphere(t.position, t.lossyScale.x * 0.5f);
        }
    }
}
