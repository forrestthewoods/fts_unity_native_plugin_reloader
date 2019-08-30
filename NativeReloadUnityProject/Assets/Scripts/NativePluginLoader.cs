using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

// Native system functions for interacting with NativePlugins
static class SystemLibrary
{
    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    static public extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static public extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32")]
    static public extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
}

// Singleton class to help with loading and unloading NativePlugins
public class NativePluginLoader : MonoBehaviour
{
    // Constants
    const string PATH = "Assets/Plugins/";
    const string EXT = ".dll";

    // Static fields
    static NativePluginLoader _singleton;

    // Private fields
    Dictionary<string, NativePlugin> _loadedPlugins = new Dictionary<string, NativePlugin>();

    // Static Properties
    static NativePluginLoader singleton {
        get {
            if (_singleton == null) {
                var go = new GameObject("PluginLoader");
                var pl = go.AddComponent<NativePluginLoader>();
                Debug.Assert(_singleton == pl); // should be set by awake
            }
            return _singleton;
        }
    }

    // Static Methods
    public static NativePlugin GetPlugin(string pluginName)
    {
        // Get singleton
        var pl = NativePluginLoader.singleton;

        // Get or load plugin
        NativePlugin result = null;
        if (!pl._loadedPlugins.TryGetValue(pluginName, out result)) {
            var plugin_path = PATH + pluginName + EXT;
            var plugin_handle = SystemLibrary.LoadLibrary(plugin_path);
            if (plugin_handle == IntPtr.Zero)
                throw new System.Exception("Failed to load plugin [" + plugin_path + "]");

            result = new NativePlugin(plugin_handle);
            pl._loadedPlugins[pluginName] = result;
        }

        return result;
    }

    // Methods
    void Awake() {
        if (_singleton != null)
            throw new System.Exception("Created PluginLoader when one already existed");

        _singleton = this;
        DontDestroyOnLoad(this.gameObject);

        LoadAll();
    }

    void OnDestroy() {
        // Free all loaded libraries
        foreach(var kvp in _loadedPlugins) {
            SystemLibrary.FreeLibrary(kvp.Value.handle);
        }
    }

    void LoadAll() {
        // Loop over all assemblies
        var asms = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // Loop over all types
            foreach (var type in assembly.GetTypes())
            {
                // Get type custom attributes
                var type_attributes = type.GetCustomAttributes(typeof(PluginAttr), true);
                if (type_attributes.Length > 0)
                {
                    var type_attribute = type_attributes[0] as PluginAttr;
                    var plugin_name = type_attribute.pluginName;
                    var plugin = NativePluginLoader.GetPlugin(plugin_name);

                    // Loop over fields
                    var fields = type.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    foreach (var field in fields)
                    {
                        // Get field custom attributes
                        var field_attributes = field.GetCustomAttributes(typeof(PluginFunctionAttr), true);
                        if (field_attributes.Length > 0)
                        {
                            // Get field attribute
                            var field_attribute = field_attributes[0] as PluginFunctionAttr;
                            var function_name = field_attribute.functionName;

                            // Get function pointer
                            var fn_ptr = plugin.GetFunction(function_name);
                            var fn_del = Marshal.GetDelegateForFunctionPointer(fn_ptr, field.FieldType);

                            // Set static field value
                            field.SetValue(null, fn_del);
                        }
                    }
                }
            }
        }
    }
}

// NativePlugin helper
public class NativePlugin {
    // TODO: Editor vs non-editor
    const string PATH = "Assets/Plugins/";

    // TODO: Multi-platform
    const string EXT = ".dll";

    // Properties
    public IntPtr handle { get; private set; }

    // Methods
    public NativePlugin(IntPtr handle) {
        this.handle = handle;
    }

    public IntPtr GetFunction(string functionName) {
        return SystemLibrary.GetProcAddress(handle, functionName);
    }
}

// Attribute for Plugin APIs
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class PluginAttr : System.Attribute
{
    // Fields
    public string pluginName { get; private set; }

    // Methods
    public PluginAttr(string pluginName)
    {
        this.pluginName = pluginName;
    }
}

// Attribute for functions inside an API
[AttributeUsage(AttributeTargets.Field)]
public class PluginFunctionAttr : System.Attribute
{
    // Fields
    public string functionName { get; private set; }

    // Methods
    public PluginFunctionAttr(string functionName)
    {
        this.functionName = functionName;
    }
}

