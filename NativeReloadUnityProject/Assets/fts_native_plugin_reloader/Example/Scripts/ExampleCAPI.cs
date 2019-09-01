using System;
using System.Runtime.InteropServices;

using fts;

// ------------------------------------------------------------------------
// Example C API defined in my_cool_plugin.h
// ------------------------------------------------------------------------
/*
extern "C" 
{
	__declspec(dllexport) int simple_func();
	__declspec(dllexport) float sum(float a, float b);
	__declspec(dllexport) int string_length(const char* s);

	struct SimpleStruct {
		int a;
		float b;
		bool c;
	};
	__declspec(dllexport) double send_struct(SimpleStruct const* ss);
	__declspec(dllexport) SimpleStruct recv_struct();
}
*/


// ------------------------------------------------------------------------
// Basic PInvoke
// ------------------------------------------------------------------------
public static class FooPlugin_PInvoke
{
    [DllImport("cpp_example_dll", EntryPoint = "simple_func")]
    extern static public int simpleFunc();

    [DllImport("cpp_example_dll", EntryPoint = "sum")]
    extern static public float sum(float a, float b);
    
    [DllImport("cpp_example_dll", EntryPoint = "string_length")]
    extern static public int stringLength([MarshalAs(UnmanagedType.LPStr)]string s);
    
    [DllImport("cpp_example_dll", EntryPoint = "send_struct")]
    extern static public double sendStruct(ref SimpleStruct ss);
    
    [DllImport("cpp_example_dll", EntryPoint = "recv_struct")]
    extern static public SimpleStruct recvStruct();
}


// ------------------------------------------------------------------------
// Auto Lookup
//
// Requires 'NativePluginLoader' object to exist in scene
// ------------------------------------------------------------------------
[PluginAttr("cpp_example_dll")]
public static class FooPluginAPI_Auto
{
    [PluginFunctionAttr("simple_func")] 
    public static SimpleFunc simpleFunc = null;
    public delegate int SimpleFunc();

    [PluginFunctionAttr("sum")] 
    public static Sum sum = null;
    public delegate float Sum(float a, float b);

    [PluginFunctionAttr("string_length")] 
    public static StringLength stringLength = null;
    public delegate int StringLength([MarshalAs(UnmanagedType.LPStr)]string s);

    [PluginFunctionAttr("send_struct")] 
    public static SendStruct sendStruct = null;
    public delegate double SendStruct(ref SimpleStruct ss);

    [PluginFunctionAttr("recv_struct")]
    public static RecvStruct recvStruct = null;
    public delegate SimpleStruct RecvStruct();
}


// ------------------------------------------------------------------------
// C Structs
// ------------------------------------------------------------------------
[StructLayout(LayoutKind.Sequential)]
public struct SimpleStruct {
    public int a;
    public float b;
    public bool c;
    
    public SimpleStruct(int a, float b, bool c) {
        this.a = a;
        this.b = b;
        this.c = c;
    }
}