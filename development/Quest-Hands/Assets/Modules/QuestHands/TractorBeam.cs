using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TractorBeam : MonoBehaviour
{
    public HandDebugInfo info;
    public float force = 1f;
    public float particleEmission = 20f;
    public LayerMask layerMask;

    public Transform visuals;

    Vector3 forceCenter => info.averageRootPos + info.handDirection * 0.2f;

    public ParticleSystem system;

    void Update() {
        var em = system.emission;
        em.rateOverTimeMultiplier = info.force * particleEmission;

        visuals.transform.position = forceCenter;
        visuals.rotation = Quaternion.LookRotation(info.handDirection);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(Physics.SphereCast(info.averageRootPos - info.handDirection * 0.2f, 0.2f, info.handDirection, out var hitInfo, Mathf.Infinity, layerMask)) {
            Debug.Log("hit! : " + hitInfo.collider.name, hitInfo.collider);

            if(hitInfo.rigidbody && !hitInfo.rigidbody.isKinematic) {
                var r = hitInfo.rigidbody;
                var offset = (r.position - forceCenter).normalized;
                r.AddForce(-offset * force * info.force, ForceMode.Force);
            }
        }
    }
}
