using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rigidfinger : MonoBehaviour
{
    Transform parent;
    Rigidbody rigidbody;


    // Start is called before the first frame update
    void Start()
    {
        parent = transform.parent;
        transform.SetParent(null);   
        rigidbody = GetComponent<Rigidbody>(); 
    }

    Vector3 parentDelta;
    Vector3 lastParentPos;
    public float forceAmount = 100;
    public RigidFingerSettings settings;
    public float frictionStrength => settings.frictionStrength;
    public float offsetStrength => settings.offsetStrength;

    // Update is called once per frame
    void FixedUpdate()
    {
        // get parent pos
        var pPos = parent.position;

        // try to get there
        var delta = pPos - rigidbody.position;

        if(delta.magnitude > 0.5f) {
            rigidbody.MovePosition(pPos);
        }

        Debug.DrawLine(pPos, rigidbody.position, Color.red);

        rigidbody.AddForce(delta * forceAmount);

        parentDelta = lastParentPos - pPos;
        lastParentPos = pPos;

        // Debug.Log(parentDelta.x.ToString("F6") + ";" + parentDelta.y.ToString("F6") + ";" + parentDelta.z.ToString("F6"));

        Debug.DrawLine(pPos, pPos + parentDelta, Color.yellow);
    }

    private void OnCollisionEnter(Collision other) {
        // Debug.Log("Enter coll: " + other.rigidbody.name, other.rigidbody);
    }

    private void OnCollisionStay(Collision other) {
        // Debug.Log("Colliding with: " + other.rigidbody.name + ": " + other.relativeVelocity + ", " + other.impulse, other.rigidbody);

        if(!other.rigidbody) return;

        Debug.DrawLine(transform.position, transform.position + other.relativeVelocity, Color.green);

        foreach(var c in other.contacts) {
            var offsetVector = parent.position - c.point;
            var offset = offsetVector.magnitude;

            // if(offset > 0.005f && offset < 0.05f)
            {
                var normalizedOffsetStrength = offset / 0.005f;
                Debug.DrawLine(c.point, c.point + c.normal, Color.blue);
                // other.rigidbody.AddForceAtPosition(c.normal * offset, c.point, ForceMode.Force);
                other.rigidbody.AddForceAtPosition((-parentDelta + offsetVector) * frictionStrength * normalizedOffsetStrength, c.point);
                other.rigidbody.AddForce(-offsetVector * offsetStrength);
                // other.rigidbody.AddForce
                // other.rigidbody.MovePosition(other.rigidbody.position - parentDelta);
            }
        }
    }
}
