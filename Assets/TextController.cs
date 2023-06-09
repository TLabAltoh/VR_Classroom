using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextController : MonoBehaviour
{
    void Start()
    {
        //this.transform.parent = null;
    }

    void Update()
    {
        this.transform.LookAt(Camera.main.transform, Vector3.up);
    }
}
