using System;
using System.Runtime.InteropServices;



public static class FooPluginAPI 
{
    [DllImport("cpp_example_dll", EntryPoint = "simple_func")]
    extern static public int test_func();
}

public static class FooPluginAPI_Lazy
{
    const string pluginName = "cpp_example_dll";




    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int TestFunc();
    static TestFunc _testFunc = null;
    public static TestFunc testFunc {
        get {
            if (_testFunc == null) {
                var fn = PluginLoader.GetPlugin(pluginName).GetFunction("simple_func");
                _testFunc = Marshal.GetDelegateForFunctionPointer<TestFunc>(fn);
            }
            return _testFunc;
        }
    }

    // Plugin wrapper
    static NativePlugin _plugin;
    static NativePlugin plugin {
        get {
            if (_plugin == null)
                _plugin = PluginLoader.GetPlugin(pluginName);
            return _plugin;
        }
    }
}
