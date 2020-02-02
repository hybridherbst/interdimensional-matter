using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TractorBeam : MonoBehaviour
{
    public HandDebugInfo info;
    public float force = 1f;
    public float tractorBeamRadius = 0.1f;
    public float particleEmission = 20f;
    public LayerMask layerMask;
    public float forceCenterDistance = 0.22f;

    public Transform visuals;

    Vector3 forceCenter => info.averageRootPos + info.handDirection * forceCenterDistance;

    public ParticleSystem system;
    public ParticleSystem linearSystem;

    void Update() {
        var em = system.emission;
        em.rateOverTimeMultiplier = info.force * particleEmission;

        var ls = linearSystem.velocityOverLifetime;
        ls.zMultiplier = -1f * (info.force * 5 + 0.5f);

        visuals.transform.position = forceCenter;
        visuals.rotation = Quaternion.LookRotation(info.handDirection);
    }

    Rigidbody lastR;
    Quaternion offsetRotation;

    // Update is called once per frame
    void FixedUpdate()
    {
        if(Physics.SphereCast(info.averageRootPos - info.handDirection * 0.1f, tractorBeamRadius, info.handDirection, out var hitInfo, Mathf.Infinity, layerMask)) {
            // Debug.Log("hit! : " + hitInfo.collider.name, hitInfo.collider);

            if(hitInfo.rigidbody && !hitInfo.rigidbody.isKinematic) {
                var r = hitInfo.rigidbody;

                if(r != lastR)
                {
                    lastR = r;
                    offsetRotation = r.rotation * Quaternion.Inverse(info.wristTransform.rotation);
                }
                var offset = (r.position - forceCenter).normalized;
                r.AddForce(-offset * force * info.force, ForceMode.Force);

                r.rotation = Quaternion.Slerp(r.rotation, info.wristTransform.rotation * offsetRotation, Time.fixedDeltaTime * 5 * info.force);
            }
        }
    }
}
