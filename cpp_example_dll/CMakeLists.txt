cmake_minimum_required(VERSION 3.0)
project(cpp_example_dll)

set(SOURCE_FILES
    c_api.cpp
    dllmain.cpp
)

add_library(cpp_example_dll SHARED ${SOURCE_FILES})

target_include_directories(cpp_example_dll PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})