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

        // Static Methods
        public static NativePlugin GetPlugin(string pluginName)
        {
            // Get singleton
            var pl = NativePluginLoader.singleton;

            // Get or load plugin
            NativePlugin result = null;
            if (!pl._loadedPlugins.TryGetValue(pluginName, out result)) {
                var pluginPath = PATH + pluginName + EXT;
                var pluginHandle = SystemLibrary.LoadLibrary(pluginPath);
                if (pluginHandle == IntPtr.Zero)
                    throw new System.Exception("Failed to load plugin [" + pluginPath + "]");

                result = new NativePlugin(pluginHandle);
                pl._loadedPlugins.Add(pluginName, result);
            }

            return result;
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

            // Free all loaded libraries
            foreach (var kvp in _loadedPlugins) {
                // TODO: _loadedPlugins may be empty if script recompiled while running
                // Need to serialize _loadedPlugins but ONLY during editor script reload
                // Maybe use ISerializationCallbackReceiver?
                Debug.Log("Freeing " + kvp.Key);
                SystemLibrary.FreeLibrary(kvp.Value.handle);
            }
            _singleton = null;
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
                    LoadPlugin(type, true);
                }
            }            
        }

        // Public LoadPlugin function for explicit loads
        public void LoadPlugin(Type type) {
            LoadPlugin(type, auto: false);
        }

        void LoadPlugin(Type pluginType, bool auto) {
            // Get custom attributes for type
            var typeAttributes = pluginType.GetCustomAttributes(typeof(PluginAttr), true);
            if (typeAttributes.Length > 0) {
                Debug.Assert(typeAttributes.Length == 1); // should not be possible

                var typeAttribute = typeAttributes[0] as PluginAttr;
                if (auto && typeAttribute.auto == false)
                    return;

                var pluginName = typeAttribute.pluginName;
                var plugin = NativePluginLoader.GetPlugin(pluginName);

                // Loop over fields in type
                var fields = pluginType.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                foreach (var field in fields) {
                    // Get custom attributes for field
                    var fieldAttributes = field.GetCustomAttributes(typeof(PluginFunctionAttr), true);
                    if (fieldAttributes.Length > 0) {
                        Debug.Assert(fieldAttributes.Length == 1); // should not be possible

                        // Get PluginFunctionAttr attribute
                        var fieldAttribute = fieldAttributes[0] as PluginFunctionAttr;
                        var functionName = fieldAttribute.functionName;

                        // Get function pointer
                        var fnPtr = plugin.GetFunction(functionName);

                        if (fnPtr != IntPtr.Zero) {
                            // Get delegate pointer
                            var fnDelegate = Marshal.GetDelegateForFunctionPointer(fnPtr, field.FieldType);

                            // Set static field value
                            field.SetValue(null, fnDelegate);
                        } else {
                            Debug.LogError(string.Format("Failed to find function [{0}] in plugin [{1}]", functionName, pluginName));
                        }
                    }
                }
            }
        }


        // ISerializationCallbackReceiver
        List<string> _serializeKeys = new List<string>();
        List<NativePlugin> _serializeValues = new List<NativePlugin>();

        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            Debug.Log("OnBeforeSerialize");

            _serializeKeys.Clear();
            _serializeValues.Clear();

            foreach (var kvp in _loadedPlugins) {
                _serializeKeys.Add(kvp.Key);
                _serializeValues.Add(kvp.Value);
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()  {
            Debug.Log("OnAfterDeserialize");
            _loadedPlugins.Clear();

            Debug.Assert(_serializeKeys.Count == _serializeValues.Count);
            for (int i = 0; i < _serializeKeys.Count; ++i) {
                _loadedPlugins.Add(_serializeKeys[i], _serializeValues[i]);
            }

            _serializeKeys.Clear();
            _serializeValues.Clear();
        }
    }


    // ------------------------------------------------------------------------
    // Small wrapper around NativePlugin helper
    // ------------------------------------------------------------------------
    [System.Serializable]
    public class NativePlugin {
        // Properties
        [UnityEngine.SerializeField] IntPtr _handle;
        public IntPtr handle { get { return _handle; } }

        // Methods
        public NativePlugin(IntPtr handle) {
            this._handle = handle;
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
        public bool auto { get; private set; }

        // Methods
        public PluginAttr(string pluginName, bool auto = true) {
            this.pluginName = pluginName;
            this.auto = auto;
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