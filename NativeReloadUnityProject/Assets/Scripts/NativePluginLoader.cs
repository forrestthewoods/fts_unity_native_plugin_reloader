using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class NativePluginLoader
{
    [DllImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32")]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
}

public class PluginLoader : MonoBehaviour
{
    // Statics
    static PluginLoader _singleton; // singleton

    // Private fields
    Dictionary<string, NativePlugin> _loadedPlugins = new Dictionary<string, NativePlugin>();

    // Properties
    static PluginLoader singleton {
        get {
            if (_singleton == null) {
                var go = new GameObject("PluginLoader");
                DontDestroyOnLoad(go); // unload plugins on destory
                var pl = go.AddComponent<PluginLoader>();
                Debug.Assert(_singleton == pl); // should be set by awake
            }
            return _singleton;
        }
    }

    // Methods
    void Awake() {
        if (_singleton != null)
            throw new System.Exception("Created PluginLoader when one already existed");

        _singleton = this;
    }

    public static NativePlugin GetPlugin(string pluginName)
    {
        var pl = PluginLoader.singleton;

        NativePlugin result = null;
        if (!pl._loadedPlugins.TryGetValue(pluginName, out result)) {
            result = new NativePlugin(pluginName);
            pl._loadedPlugins[pluginName] = result;
        }

        return result;
    }

    void OnDestroy() {
        foreach(var kvp in _loadedPlugins) {
            NativePluginLoader.FreeLibrary(kvp.Value.handle);
        }
    }
}

public class NativePlugin {
    const string PATH = "Assets/Plugins/";
    const string EXT = ".dll";

    IntPtr _handle;
    string _name;

    public IntPtr handle { get { return _handle; } }

    public NativePlugin(string pluginName) {
        _name = pluginName;
        var path = PATH + pluginName + EXT;
        _handle = NativePluginLoader.LoadLibrary(path);
        if (_handle == IntPtr.Zero)
            throw new System.Exception("Failed to load plugin [" + path + "]");
    }

    public IntPtr GetFunction(string functionName) {
        return NativePluginLoader.GetProcAddress(_handle, functionName);
    }
}

public class NativePluginFunction
{
    IntPtr _functionPtr;

    public IntPtr functionPtr { get { return _functionPtr; } }

    public NativePluginFunction(string pluginName, string functionName) {
        _functionPtr = PluginLoader.GetPlugin(pluginName).GetFunction(functionName);
    }
}
