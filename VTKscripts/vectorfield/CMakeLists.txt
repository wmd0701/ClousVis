cmake_minimum_required(VERSION 3.8...3.12 FATAL_ERROR)
 
PROJECT(TransformData)

find_package(VTK REQUIRED)

add_executable(${PROJECT_NAME} MACOSX_BUNDLE ${PROJECT_NAME}.cpp)
target_link_libraries(${PROJECT_NAME} 
	PRIVATE
		${VTK_LIBRARIES})
vtk_module_autoinit(
  TARGETS ${PROJECT_NAME}
  MODULES ${VTK_LIBRARIES})