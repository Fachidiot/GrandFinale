# CMake에게 프로젝트를 어떻게 빌드해야 하는지 알려주는 설명서 파일

# CMake 최소 요구 버전
# Minimum required version of CMake
cmake_minimum_required(VERSION 3.10)

# 프로젝트 이름 설정
# Set the project name
project(LobbyServer)

# C++ 17 표준 사용
# Use the C++17 standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# [mac] homebrew로 설치한 패키지
# find_package(nlohmann_json 3 REQUIRED)

# 실행 파일 생성
# Create the executable
add_executable(lobby_server main.cpp Server.cpp Session.cpp)

# [mac] 라이브러리 링크
# target_link_libraries(lobby_server PRIVATE nlohmann_json::nlohmann_json)

# 프로젝트에 헤더 파일 검색 경로 추가
# 이렇게 하면 소스 코드에서 #include <nlohmann/json.hpp> 를 그대로 사용할 수 있습니다.
# This allows you to use #include <nlohmann/json.hpp> in your source code as is.
target_include_directories(lobby_server PRIVATE ${CMAKE_CURRENT_SOURCE_DIR}/libs)

# 터미널 명령어
# mkdir build
# cmake ..
# make
# ./lobby_server