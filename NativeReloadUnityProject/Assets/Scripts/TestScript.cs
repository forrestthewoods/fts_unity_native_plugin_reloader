using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //var val = FooPluginAPI.test_func();
        var lazy_val = FooPluginAPI_Lazy.testFunc();
        Debug.Log("Value from DLL: " + lazy_val.ToString());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
