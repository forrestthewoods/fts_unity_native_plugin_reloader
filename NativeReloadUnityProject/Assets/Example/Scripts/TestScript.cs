using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // #1 - PInvoke
        //var val = FooPlugin_PInvoke.test_func();

        // #2 - Lazy load
        //var val = FooPluginAPI_Lazy.testFunc();

        // #3 - Auto load
        var val = FooPluginAPI_Auto._testFunc();

        Debug.Log("Value from DLL: " + val.ToString());
    }
}
