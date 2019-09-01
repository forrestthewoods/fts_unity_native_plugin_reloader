#include "c_api.h"

// Implementation can use whatever C++ you want.
// #include <vector>
// #include <windows.h>

int simple_func() {
	return 42;
}

float sum(float a, float b) {
	return a + b;
}

int string_length(const char* s) {
	int count = 0;
	while (*s++ != '\0') {
		++count;
	}
	return count;
}

double send_struct(SimpleStruct const* ss) {
	return ss 
		? (double)ss->a + ss->b + (double)ss->c
		: -1.0;
}

SimpleStruct recv_struct() {
	SimpleStruct result;
	result.a = 42;
	result.b = 1337.0;
	result.c = true;
	return result;
}
