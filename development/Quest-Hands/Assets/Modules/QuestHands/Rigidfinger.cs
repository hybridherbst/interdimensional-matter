using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rigidfinger : MonoBehaviour
{
    Transform parent;
    Rigidbody rigidbody;

    public float forceAmount = 100; 

    // Start is called before the first frame update
    void Start()
    {
        parent = transform.parent;
        transform.SetParent(null);   
        rigidbody = GetComponent<Rigidbody>(); 
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // get parent pos
        var pPos = parent.position;

        // try to get there
        var delta = pPos - rigidbody.position;

        Debug.DrawLine(pPos, rigidbody.position, Color.red);

        rigidbody.AddForce(delta * forceAmount);
    }

    private void OnCollisionEnter(Collision other) {
        
    }
}
