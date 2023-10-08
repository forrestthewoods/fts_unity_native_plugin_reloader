cmake_minimum_required(VERSION 3.5)
project(cpp_example_dll)

set(SOURCE_FILES
    c_api.cpp
    dllmain.cpp
)

add_library(cpp_example_dll SHARED ${SOURCE_FILES})

set_target_properties(cpp_example_dll PROPERTIES
    PREFIX ""
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/../NativeReloadUnityProject/Assets/Plugins/"
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/../NativeReloadUnityProject/Assets/Plugins/"
)

target_include_directories(cpp_example_dll PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})