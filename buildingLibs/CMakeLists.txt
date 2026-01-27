cmake_minimum_required(VERSION 3.10)
project(EyeBridgeProject)

# Указываем путь к вашей собранной OpenCV (где лежит OpenCVConfig.cmake)
# Обычно это папка 'build' или 'install' из вашего прошлого шага
set(OpenCV_DIR "C:/Users/Constantin Nauman/opencvBuild/building/build") 
find_package(OpenCV REQUIRED)

add_library(EyeBridge SHARED EyeBridge.cpp)

# Линкуем с вашей libopencv_world
target_link_libraries(EyeBridge ${OpenCV_LIBRARIES})