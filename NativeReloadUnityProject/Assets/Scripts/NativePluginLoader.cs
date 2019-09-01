using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace fts_plugin_loader
{
    // ------------------------------------------------------------------------
    // Native API for loading/unloading NativePlugins
    //
    // TODO: Handle non-Windows platforms
    // ------------------------------------------------------------------------
    static class SystemLibrary
    {
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        static public extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static public extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32")]
        static public extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        static public extern uint GetLastError();
    }


    // ------------------------------------------------------------------------
    // Singleton class to help with loading and unloading of native plugins
    // ------------------------------------------------------------------------
    [System.Serializable]
    public class NativePluginLoader : MonoBehaviour, ISerializationCallbackReceiver
    {
        // Constants
        const string PATH = "Assets/Plugins/"; // TODO: Handle non-editor builds
        const string EXT = ".dll"; // TODO: Handle different platforms

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

        // Methods
        void Awake() {
            Debug.Log("Awake");

            if (_singleton != null)
            {
                Debug.LogError(
                    string.Format("Created multiple NativePluginLoader objects. Destroying duplicate created on GameObject [{0}]",
                    this.gameObject.name));
                Destroy(this);
                return;
            }

            _singleton = this;
            DontDestroyOnLoad(this.gameObject);

            LoadAll();
        }

        void OnDestroy() {
            Debug.Log("OnDestroy");
            UnloadAll();
            _singleton = null;
        }

        void UnloadAll() {
            // Free all loaded libraries
            foreach (var kvp in _loadedPlugins)
            {
                // TODO: _loadedPlugins may be empty if script recompiled while running
                // Need to serialize _loadedPlugins but ONLY during editor script reload
                // Maybe use ISerializationCallbackReceiver?
                Debug.Log("Freeing " + kvp.Key);
                bool result = SystemLibrary.FreeLibrary(kvp.Value.handle);
                Debug.Log(string.Format("Freeing [{0}] - Result: [{1}]", kvp.Key, result));
            }
            _loadedPlugins.Clear();
        }

        // Load all plugins with 'PluginAttr'
        // Load all functions with 'PluginFunctionAttr'
        void LoadAll() {
            // TODO: Don't iterate all assemblies and all types
            // TODO: Could this loop over just Assembly-CSharp.dll?

            // Loop over all assemblies
            var asms = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                // Loop over all types
                foreach (var type in assembly.GetTypes()) {
                    // Get custom attributes for type
                    var typeAttributes = type.GetCustomAttributes(typeof(PluginAttr), true);
                    if (typeAttributes.Length > 0)
                    {
                        Debug.Assert(typeAttributes.Length == 1); // should not be possible

                        var typeAttribute = typeAttributes[0] as PluginAttr;

                        var pluginName = typeAttribute.pluginName;
                        NativePlugin plugin = null;
                        if (!_loadedPlugins.TryGetValue(pluginName, out plugin)) {
                            var pluginPath = PATH + pluginName + EXT;
                            Debug.Log(string.Format("Loading: {0}", pluginName));
                            var pluginHandle = SystemLibrary.LoadLibrary(pluginPath);
                            if (pluginHandle == IntPtr.Zero)
                                throw new System.Exception("Failed to load plugin [" + pluginPath + "]");

                            plugin = new NativePlugin(type, pluginHandle, pluginName);
                            _loadedPlugins.Add(pluginName, plugin);
                        }

                        LoadOne(plugin);
                    }
                }
            }            
        }

        void LoadOne(NativePlugin plugin) {
            Type type = plugin.type;

            // Loop over fields in type
            var fields = type.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            foreach (var field in fields)
            {
                // Get custom attributes for field
                var fieldAttributes = field.GetCustomAttributes(typeof(PluginFunctionAttr), true);
                if (fieldAttributes.Length > 0)
                {
                    Debug.Assert(fieldAttributes.Length == 1); // should not be possible

                    // Get PluginFunctionAttr attribute
                    var fieldAttribute = fieldAttributes[0] as PluginFunctionAttr;
                    var functionName = fieldAttribute.functionName;

                    // Get function pointer
                    var fnPtr = plugin.GetFunction(functionName);
                    if (fnPtr == IntPtr.Zero) {
                        Debug.LogError(string.Format("Failed to find function [{0}] in plugin [{1}]. Err: [{2}]", functionName, plugin.name, SystemLibrary.GetLastError()));
                        continue;
                    }

                    // Get delegate pointer
                    var fnDelegate = Marshal.GetDelegateForFunctionPointer(fnPtr, field.FieldType);

                    // Set static field value
                    field.SetValue(null, fnDelegate);
                }
            }
        }


        // It is *strongly* recommended to set Editor->Preferences->Script Changes While Playing = Recompile After Finished Playing
        // Properly support reload of native assemblies requires extra work.
        // However the following code will re-fixup delegates.
        // More importantly, it prevents a dangling DLL which results in a mandatory Editor reboot
        bool _reload = false;
        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            if (_loadedPlugins.Count > 0) {
                UnloadAll();
                _reload = true;
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()  {
            if (_reload) { 
                LoadAll();
                _reload = false;
            }
        }
    }


    // ------------------------------------------------------------------------
    // Small wrapper around NativePlugin helper
    // ------------------------------------------------------------------------
    [System.Serializable]
    public class NativePlugin {
        // Fields
        [SerializeField] IntPtr _handle;
        [SerializeField] string _typeName;
        [SerializeField] string _name;
        Type _type;

        // Properties
        public IntPtr handle { get { return _handle; } }
        public string name { get { return _name; } }
        public Type type {
            get {
                if (_type == null)
                    _type = Type.GetType(_typeName);
                return _type;
            }
        }

        // Methods
        public NativePlugin(Type type, IntPtr handle, string name) {
            this._typeName = type.Name;
            this._handle = handle;
            this._name = name;
        }

        public IntPtr GetFunction(string functionName) {
            return SystemLibrary.GetProcAddress(handle, functionName);
        }
    }

    // ------------------------------------------------------------------------
    // Attribute for Plugin APIs
    // ------------------------------------------------------------------------
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class PluginAttr : System.Attribute
    {
        // Fields
        public string pluginName { get; private set; }

        // Methods
        public PluginAttr(string pluginName) {
            this.pluginName = pluginName;
        }
    }

    // ------------------------------------------------------------------------
    // Attribute for functions inside a Plugin API
    // ------------------------------------------------------------------------
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class PluginFunctionAttr : System.Attribute
    {
        // Fields
        public string functionName { get; private set; }

        // Methods
        public PluginFunctionAttr(string functionName) {
            this.functionName = functionName;
        }
    }

} // namespace fts_plugin_loader