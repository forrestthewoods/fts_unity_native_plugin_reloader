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

    // TODO:
    // Create attribute that goes on delegate with function name
    // Create loader that goes through class and loads all delegates

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int TestFunc();
    static TestFunc _testFunc = null;
    public static TestFunc testFunc {
        get {
            if (_testFunc == null) {
                var fn = NativePluginLoader.GetPlugin(pluginName).GetFunction("simple_func");
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
                _plugin = NativePluginLoader.GetPlugin(pluginName);
            return _plugin;
        }
    }
}



[PluginAttr("cpp_example_dll")]
public static class FooPluginAPIAuto
{
    [PluginFunctionAttr("simple_func")] 
    public static TestFunc _testFunc = null;
    public delegate int TestFunc();
}
