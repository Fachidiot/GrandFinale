#pragma once

#include <string>

// 3D vector
struct vec3 {
    float x, y, z;
};

// 클라이언트 정보를 담는 구조체
struct Player
{
    std::string id;
    std::string nickname;
    int room_id = -1;      // -1 : 방 없음.
    bool is_ready = false; // 준비 상태.
    vec3 position = {0, 0, 0};
};
