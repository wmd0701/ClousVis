cmake_minimum_required(VERSION 3.8...3.12 FATAL_ERROR)
 
PROJECT(VTKScripts)

find_package(VTK REQUIRED)

add_subdirectory(combineCloudData)
add_subdirectory(createVectorfield)
