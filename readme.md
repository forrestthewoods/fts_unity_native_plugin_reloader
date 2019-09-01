# fts_unity_native_reload

fts_unity_native_reload is a single C# file that helps Unity auto-reload native plugins.

The only file you need is [NativePluginLoader.cs](NativeReloadUnityProject/Assets/fts_native_plugin_reloader/Scripts/NativePluginLoader.cs). Everything else exists only to provide a complete example.

## The Problem
Unity doesn't unload DLLs when using [PInvoke](https://docs.microsoft.com/en-us/cpp/dotnet/how-to-call-native-dlls-from-managed-code-using-pinvoke?view=vs-2019). This makes developing [Native Plugins](https://docs.unity3d.com/Manual/NativePlugins.html) a huge pain in the ass. Everytime you want to update the DLL you need to close the Unity Editor or else you get a "file in use" error.

This project solves that problem with a simple ~200 line file.

Consider the following:

``` C++
// my_cool_header.h
extern "C" {
    __declspec(dllexport) float sum(float a, float b);
}
```

The standard way to call this NativePlugin is:

``` Csharp
// The "old" crappy PInvoke
public static class FooPlugin_PInvoke {
    [DllImport("cpp_example_dll", EntryPoint = "sum")]
    extern static public float sum(float a, float b);
}
```

My new and improved way:

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
    float t = FooPlugin_PInvoke.sum(1.0, 2.0); // also works
}
```

Tada! For this to work all you have to do is add the `NativePluginLoader` script to your scene.

## How It Works
`NativePluginLoader.Awake` scans all assemblies for classes with the `PluginAttr` attribute. It calls `LoadLibrary` with the specified plugin name.

Next, it loops over all public static fields with the `PluginFunctionAttr` attribute. For each field it calls `GetProcAddress` and stores the result in the delegate. I wish I could declare the delegate signature and delegate variable in one line. :(

`NativePluginLoader.OnDestory` calls `FreeLibrary` for all loaded plugins. The DLLs can then be updated. Next time you run the new DLL will be loaded and the new proc addresses will be found.

The syntax for calling the delegates is identical to using PInvoke. If performance is a concern you can provide a dynamic version inside `#if UNITY_EDITOR` and rely on PInvoke for standalone builds.

## Platforms
This currently only supports Windows. Supporting other platforms should be trivial. Pull requests welcome.

## License
Entire repo is dual-licensed under both MIT License and Unlicense.