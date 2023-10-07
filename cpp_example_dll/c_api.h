#pragma once
#ifndef UNITY_INTERFACE_API
    #if defined(_MSC_VER)
        #define UNITY_INTERFACE_API __stdcall
    #elif defined(__GNUC__)
        #define UNITY_INTERFACE_API
    #endif
#endif

#ifndef UNITY_INTERFACE_EXPORT
    #if defined(_MSC_VER)
        #define UNITY_INTERFACE_EXPORT __declspec(dllexport)
    #elif defined(__GNUC__)
        #define UNITY_INTERFACE_EXPORT __attribute__ ((visibility ("default")))
    #endif
#endif

// API must be "strict" C
// It is STRONGLY encourage that these functions should NEVER
// crash, throw, abort, or terminate.
#ifdef __cplusplus
extern "C" 
{
#endif
	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API simple_func();
	UNITY_INTERFACE_EXPORT float UNITY_INTERFACE_API sum(float a, float b);
	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API string_length(const char* s);

	struct SimpleStruct {
		int a;
		float b;
		bool c;
	};
	UNITY_INTERFACE_EXPORT double UNITY_INTERFACE_API send_struct(SimpleStruct const* ss);
	UNITY_INTERFACE_EXPORT SimpleStruct UNITY_INTERFACE_API recv_struct();
#ifdef __cplusplus
}
#endif
