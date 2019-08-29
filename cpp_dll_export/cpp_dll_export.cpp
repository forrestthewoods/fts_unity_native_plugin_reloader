// cpp_dll_export.cpp : Defines the exported functions for the DLL.
//

#include "pch.h"
#include "framework.h"
#include "cpp_dll_export.h"


// This is an example of an exported variable
CPPDLLEXPORT_API int ncppdllexport=0;

// This is an example of an exported function.
CPPDLLEXPORT_API int fncppdllexport(void)
{
    return 0;
}

// This is the constructor of a class that has been exported.
Ccppdllexport::Ccppdllexport()
{
    return;
}
