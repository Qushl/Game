cmake_minimum_required(VERSION 3.16)
project(TopDownHighwayDrifter LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# ========== Pure logic version (console) ==========
add_executable(drifter_logic
    src/main.cpp
)
target_include_directories(drifter_logic PRIVATE include)

# ========== Raylib version (with rendering) ==========
add_executable(drifter
    src/main_raylib.cpp
)
target_include_directories(drifter PRIVATE include)

# Find Raylib
find_package(raylib QUIET)
if(NOT raylib_FOUND)
    # Fallback for MSYS2 / MinGW
    find_path(RAYLIB_INCLUDE_DIR raylib.h
        PATHS
            C:/msys64/ucrt64/include
            /ucrt64/include
            /mingw64/include
    )
    find_library(RAYLIB_LIBRARY
        NAMES raylib
        PATHS
            C:/msys64/ucrt64/lib
            /ucrt64/lib
            /mingw64/lib
    )
    if(RAYLIB_INCLUDE_DIR AND RAYLIB_LIBRARY)
        target_include_directories(drifter PRIVATE ${RAYLIB_INCLUDE_DIR})
        target_link_libraries(drifter PRIVATE ${RAYLIB_LIBRARY})
    else()
        message(FATAL_ERROR "Raylib not found. Install with: pacman -S mingw-w64-ucrt-x86_64-raylib")
    endif()
else()
    target_link_libraries(drifter PRIVATE raylib)
endif()

# On Windows with MinGW we often need these
if(WIN32)
    target_link_libraries(drifter PRIVATE opengl32 gdi32 winmm)
endif()

# Warnings
if(MSVC)
    target_compile_options(drifter_logic PRIVATE /W4)
    target_compile_options(drifter PRIVATE /W4)
else()
    target_compile_options(drifter_logic PRIVATE -Wall -Wextra -Wpedantic)
    target_compile_options(drifter PRIVATE -Wall -Wextra -Wpedantic)
endif()
