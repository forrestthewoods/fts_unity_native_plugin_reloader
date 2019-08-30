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
                DontDestroyOnLoad(go);
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
    }

    void OnDestroy() {
        // Free all loaded libraries
        foreach(var kvp in _loadedPlugins) {
            SystemLibrary.FreeLibrary(kvp.Value.handle);
        }
    }

    void LoadAll() {

    }
}

public class NativePlugin {
    const string PATH = "Assets/Plugins/";
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


