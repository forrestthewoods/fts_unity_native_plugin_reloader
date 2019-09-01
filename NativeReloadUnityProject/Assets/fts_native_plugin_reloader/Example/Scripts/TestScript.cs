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
        //var val = FooPluginAPI_Auto._testFunc();

        //Debug.Log("Value from DLL: " + val.ToString());

        CallAutoAPI();
    }

    public void CallAutoAPI() {
        Debug.Log("Calling Auto API");

        var val = FooPluginAPI_Auto.simpleFunc();
        Debug.Log(string.Format("simple_func: {0}", val));

        var sum = FooPluginAPI_Auto.sum(2.3f, 1.2f);
        Debug.Log(string.Format("sum: {0}", sum));

        string some_string = "HelloWorld!";
        var len = FooPluginAPI_Auto.stringLength(some_string);
        Debug.Log(string.Format("Length of [{0}] is {1}", some_string, len));

        var ss = new SimpleStruct(23, 42.5f, true);
        var result = FooPluginAPI_Auto.sendStruct(ref ss);
        Debug.Log(string.Format("SendStruct result [{0}]", result));

        ss = FooPluginAPI_Auto.recvStruct();
        result = FooPluginAPI_Auto.sendStruct(ref ss);
        Debug.Log(string.Format("RecvStruct result [{0}]", result));
    }
}

// TODO: UnityEditor button to decipher reload behavior
#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(TestScript))]
class DecalMeshHelperEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        var ts = target as TestScript;

        if (GUILayout.Button("Call Auto API"))
            ts.CallAutoAPI();
    }
}
#endif