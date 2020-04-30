using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class GetGeom : MonoBehaviour
{
    public OVRBoundary.BoundaryType type = OVRBoundary.BoundaryType.OuterBoundary;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public Vector3[] bound;
    public List<Vector3> bound2;
List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
public int subsCount = -1;

public bool conf;
public Vector3 dim;

    // Update is called once per frame
    void Update()
    {
        conf = OVRManager.boundary.GetConfigured();
        dim = OVRManager.boundary.GetDimensions(type);

        bound = OVRManager.boundary.GetGeometry(type);
        var res = OVRManager.boundary.TestPoint(Vector3.zero, type);
        Debug.Log(res.ClosestPoint);

        SubsystemManager.GetInstances<XRInputSubsystem>(subsystems);
        subsCount = subsystems.Count;
        foreach(var i in subsystems) {
            i.TryGetBoundaryPoints(bound2);
        }
    }
}
