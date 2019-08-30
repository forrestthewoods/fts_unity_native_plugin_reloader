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

[AttributeUsage(AttributeTargets.Class)]
public class PluginAutoLoadAttr : System.Attribute
{
    // Fields
    public string pluginName { get; private set; }

    // Methods
    public PluginAutoLoadAttr(string pluginName) {
        this.pluginName = pluginName;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class PluginFunctionAutoLoadAttr : System.Attribute
{
    // Fields
    public string functionName { get; private set; }

    // Methods
    public PluginFunctionAutoLoadAttr(string functionName) {
        this.functionName = functionName;
    }
}


[PluginAutoLoadAttr("cpp_example_dll")]
public static class FooPluginAPIAuto
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate int TestFunc();
    [PluginFunctionAutoLoadAttr("simple_func")] public static TestFunc _testFunc = null;
}

public static class HACK
{
    public static void LoadAll()
    {
        // Loop over all assemblies
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            // Loop over all types
            foreach (var type in assembly.GetTypes())
            {
                // Get type custom attributes
                var type_attributes = type.GetCustomAttributes(typeof(PluginAutoLoadAttr), true);
                if (type_attributes.Length > 0) {
                    var type_attribute = type_attributes[0] as PluginAutoLoadAttr;
                    var plugin_name = type_attribute.pluginName;

                    var plugin = PluginLoader.GetPlugin(plugin_name);

                    // Loop over fields
                    var fields = type.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    foreach (var field in fields) {

                        // Get field custom attributes
                        var field_attributes = field.GetCustomAttributes(typeof(PluginFunctionAutoLoadAttr), true);
                        if (field_attributes.Length > 0) {
                            var field_attribute = field_attributes[0] as PluginFunctionAutoLoadAttr;
                            var function_name = field_attribute.functionName;

                            var fn = plugin.GetFunction(function_name);
                            var fn_ptr = Marshal.GetDelegateForFunctionPointer(fn, field.FieldType);

                            field.SetValue(null, fn_ptr);
                        }
                    }
                }
            }
        }
    }
}