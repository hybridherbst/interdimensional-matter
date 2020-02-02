using System.Collections;
using System.Collections.Generic;
using OculusSampleFramework;
using UnityEngine;
using UnityEngine.Events;

public class CustomPoke : MonoBehaviour
{
    public float localY;
    public float activationMinY = -0.1f;
    public float resetMinY = -0.04f;

    private void Start() {
        
    }

    bool pressed = false;

    public UnityEvent OnPressed; 

    private void Update() {
        transform.localRotation = Quaternion.identity;
        localY = transform.localPosition.y;

        if(localY < activationMinY && !pressed) {
            Debug.Log("OHMYGOD BUTTON PRESSED");
            OnPressed.Invoke();
            pressed = true;
        }

        if(localY > resetMinY && pressed)
        {
            pressed = false;
        }
    }
}
