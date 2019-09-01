# fts_unity_native_reload

fts_unity_native_reload is a single C# file that helps Unity auto-reload native plugins.

The only file you need is [NativePluginLoader.cs](NativeReloadUnityProject/Assets/fts_native_plugin_reloader/Scripts/NativePluginLoader.cs). The rest of the repository is just example usage.

## The Problem
Unity doesn't unload DLLs when using [PInvoke](https://docs.microsoft.com/en-us/cpp/dotnet/how-to-call-native-dlls-from-managed-code-using-pinvoke?view=vs-2019). This makes developing [Native Plugins](https://docs.unity3d.com/Manual/NativePlugins.html) a huge pain in the ass. Everytime you want to update the DLL you need to close the Unity Editor or else you get a "file in use" error.

This project solves that problem with a simple ~200 line file.

``` C++
// my_cool_cpp_header.h
extern "C" {
    __declspec(dllexport) float sum(float a, float b);
}
```

The standard way to call this NativePlugin.

``` Csharp
// The "old" crappy PInvoke
public static class FooPlugin_PInvoke {
    [DllImport("cpp_example_dll", EntryPoint = "sum")]
    extern static public float sum(float a, float b);
}
```

My new and improved way.

``` Csharp
[PluginAttr("cpp_example_dll")]
public static class FooPlugin
{
    [PluginFunctionAttr("sum")] 
    public static Sum sum = null;
    public delegate float Sum(float a, float b);
}

void CoolFunc() {
    float s = FooPlugin.sum(1.0, 2.0);
}
```

Tada! For this to work all you have to do is add the `NativePluginLoader` script to your scene.

## How It Works
`NativePluginLoader` scans all assemblies for classes with the `PluginAttr` attribute. It calls `LoadLibrary` for the specified plugin name.

Next it loops over all public static fields with the `PluginFunctionAttr` attribute. For each field it calls `GetProcAddress` and stores the result in the delegate. I wish I could declare the delegate signature and delegate variable in one line, but alas.

When the `NativePluginLoader` component is destroyed it calls `FreeLibrary`.

## License
Entire repo is dual-licensed under both MIT License and Unlicense.