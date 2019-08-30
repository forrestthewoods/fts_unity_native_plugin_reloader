using System;
using System.Runtime.InteropServices;


// Basic PInvoke
public static class FooPluginAPI 
{
    [DllImport("cpp_example_dll", EntryPoint = "simple_func")]
    extern static public int test_func();
}

// Lazy look-up, done manually
public static class FooPluginAPI_Lazy
{
    const string pluginName = "cpp_example_dll";

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

// AutoLoad on start
[PluginAttr("cpp_example_dll")]
public static class FooPluginAPIAuto
{
    [PluginFunctionAttr("simple_func")] 
    public static TestFunc _testFunc = null;
    public delegate int TestFunc();
}
