// The following ifdef block is the standard way of creating macros which make exporting
// from a DLL simpler. All files within this DLL are compiled with the CPPDLLEXPORT_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see
// CPPDLLEXPORT_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef CPPDLLEXPORT_EXPORTS
#define CPPDLLEXPORT_API __declspec(dllexport)
#else
#define CPPDLLEXPORT_API __declspec(dllimport)
#endif

// This class is exported from the dll
class CPPDLLEXPORT_API Ccppdllexport {
public:
	Ccppdllexport(void);
	// TODO: add your methods here.
};

extern CPPDLLEXPORT_API int ncppdllexport;

CPPDLLEXPORT_API int fncppdllexport(void);
