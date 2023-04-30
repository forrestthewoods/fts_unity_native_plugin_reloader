// LICENSE
//  See end of file for license information.
//
// AUTHOR
//   Forrest Smith

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace fts
{

    // ------------------------------------------------------------------------
    // Native API for loading/unloading NativePlugins
    //
    // ------------------------------------------------------------------------
    static class SystemLibrary
    {
        public static IntPtr LoadLib(string fileName)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            IntPtr hLib = LoadLibrary(fileName);
            var errID = GetLastError();
            if (hLib == IntPtr.Zero) {
                Debug.LogError(string.Format("Failed to load library [{0}]. Err: [{1}]", fileName, errID));
            }
            return hLib;
#else            
            const int RTLD_NOW = 2;
            IntPtr hLib = dlopen(fileName, RTLD_NOW);
            var errPtr = dlerror();
            if (errPtr != IntPtr.Zero) {
                Debug.LogError(Marshal.PtrToStringAnsi(errPtr));
            }
            else
            {
                Debug.Log($"Loaded {fileName}");
            }
            return hLib;
#endif
        }

        public static void FreeLib(IntPtr hLib)
        {
            if (hLib == IntPtr.Zero)
                return;
            
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            FreeLibrary(hLib);
#else
            dlclose(hLib);
#endif
            return;
        }

        public static IntPtr GetAddress(IntPtr hLib, string fnName)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            IntPtr fnPtr = GetProcAddress(hLib, fnName);
            var errID = GetLastError();
            if (fnPtr == IntPtr.Zero) {
                Debug.LogError(string.Format("Failed to find function [{0}] in library [{1}]. Err: [{2}]", fnName, fileName, errID));
            }
            return fnPtr;
            
#else
            IntPtr fnPtr = dlsym(hLib, fnName);
            var errPtr = dlerror();
            if (errPtr != IntPtr.Zero) {
                Debug.LogError(Marshal.PtrToStringAnsi(errPtr));
            }
            else
            {
                Debug.Log($"Found symbol {fnName}");
            }
            return fnPtr;
#endif
        }
        
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        static private extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static private extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32")]
        static private extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        static private extern uint GetLastError();
#else
        [DllImport ("__Internal")]
        private static extern IntPtr dlopen(String fileName, int flags);

        [DllImport ("__Internal")]
        private static extern IntPtr dlsym(IntPtr handle, String symbol);

        [DllImport ("__Internal")]
        private static extern int dlclose(IntPtr handle);

        [DllImport ("__Internal")]
        private static extern IntPtr dlerror();
#endif
    }


    // ------------------------------------------------------------------------
    // Singleton class to help with loading and unloading of native plugins
    // ------------------------------------------------------------------------
    [System.Serializable]
    public class NativePluginLoader : MonoBehaviour, ISerializationCallbackReceiver
    {
        // Constants
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        const string EXT = ".dll"; 
#else
        const string EXT = ".dylib"; 
#endif
        // Static fields
        static NativePluginLoader _singleton;

        // Private fields
        Dictionary<string, IntPtr> _loadedPlugins = new Dictionary<string, IntPtr>();
        string _path;

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
            _path = Application.dataPath + "/Plugins/";

            LoadAll();
        }

        void OnDestroy() {
            UnloadAll();
            _singleton = null;
        }

        // Free all loaded libraries
        void UnloadAll()
        {
            foreach (var kvp in _loadedPlugins) {
                SystemLibrary.FreeLib(kvp.Value);
            }
            _loadedPlugins.Clear();
        }

        // Load all plugins with 'PluginAttr'
        // Load all functions with 'PluginFunctionAttr'
        void LoadAll() {
            // TODO: Could loop over just Assembly-CSharp.dll in most cases?

            // Loop over all assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies) {
                // Loop over all types
                foreach (var type in assembly.GetTypes()) {
                    // Get custom attributes for type
                    var typeAttributes = type.GetCustomAttributes(typeof(PluginAttr), true);
                    if (typeAttributes.Length > 0)
                    {
                        Debug.Assert(typeAttributes.Length == 1); // should not be possible

                        var typeAttribute = typeAttributes[0] as PluginAttr;

                        var pluginName = typeAttribute.pluginName;
                        IntPtr pluginHandle = IntPtr.Zero;
                        if (!_loadedPlugins.TryGetValue(pluginName, out pluginHandle)) {
                            var pluginPath = _path + pluginName + EXT;
                            pluginHandle = SystemLibrary.LoadLib(pluginPath);
                            if (pluginHandle == IntPtr.Zero)
                                throw new System.Exception("Failed to load plugin [" + pluginPath + "]");

                            _loadedPlugins.Add(pluginName, pluginHandle);
                        }

                        // Loop over fields in type
                        var fields = type.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                        foreach (var field in fields) {
                            // Get custom attributes for field
                            var fieldAttributes = field.GetCustomAttributes(typeof(PluginFunctionAttr), true);
                            if (fieldAttributes.Length > 0) {
                                Debug.Assert(fieldAttributes.Length == 1); // should not be possible

                                // Get PluginFunctionAttr attribute
                                var fieldAttribute = fieldAttributes[0] as PluginFunctionAttr;
                                var functionName = fieldAttribute.functionName;

                                // Get function pointer
                                var fnPtr = SystemLibrary.GetAddress(pluginHandle, functionName);
                                if (fnPtr == IntPtr.Zero)
                                    continue;

                                // Get delegate pointer
                                var fnDelegate = Marshal.GetDelegateForFunctionPointer(fnPtr, field.FieldType);

                                // Set static field value
                                field.SetValue(null, fnDelegate);
                            }
                        }
                    }
                }
            }            
        }


        // It is *strongly* recommended to set Editor->Preferences->Script Changes While Playing = Recompile After Finished Playing
        // Properly support reload of native assemblies requires extra work.
        // However the following code will re-fixup delegates.
        // More importantly, it prevents a dangling DLL which results in a mandatory Editor reboot
        bool _reloadAfterDeserialize = false;
        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            if (_loadedPlugins.Count > 0) {
                UnloadAll();
                _reloadAfterDeserialize = true;
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()  {
            if (_reloadAfterDeserialize) { 
                LoadAll();
                _reloadAfterDeserialize = false;
            }
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

} // namespace fts

/*
------------------------------------------------------------------------------
This software is available under 2 licenses -- choose whichever you prefer.
------------------------------------------------------------------------------
ALTERNATIVE A - The MIT License (MIT)

Copyright (c) 2019 Forrest Smith

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
------------------------------------------------------------------------------
ALTERNATIVE B - Public Domain (www.unlicense.org)

This is free and unencumbered software released into the public domain.

Anyone is free to copy, modify, publish, use, compile, sell, or
distribute this software, either in source code form or as a compiled
binary, for any purpose, commercial or non-commercial, and by any
means.

In jurisdictions that recognize copyright laws, the author or authors
of this software dedicate any and all copyright interest in the
software to the public domain.We make this dedication for the benefit
of the public at large and to the detriment of our heirs and
successors.We intend this dedication to be an overt act of
relinquishment in perpetuity of all present and future rights to this
software under copyright law.


THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

For more information, please refer to<http://unlicense.org/>
------------------------------------------------------------------------------
*/