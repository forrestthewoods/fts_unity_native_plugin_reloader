using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var val = CAPI.test_func();
        Debug.Log("Value from DLL: " + val.ToString());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
