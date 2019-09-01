#pragma once

// API must be "strict" C
// It is STRONGLY encourage that these functions should NEVER
// crash, throw, abort, or terminate.
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
