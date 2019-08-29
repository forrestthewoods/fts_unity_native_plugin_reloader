using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class CAPI 
{
    [DllImport("cpp_example_dll", EntryPoint = "simple_func")]
    extern static public int test_func();
}
