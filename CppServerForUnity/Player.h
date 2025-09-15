#pragma once
#include "stdafx.h"

// Vector3
struct vec3 {
    float x = 0.0f, y = 0.0f, z = 0.0f;
};

// Quaternion
struct quat {
    float x = 0.0f, y = 0.0f, z = 0.0f, w = 1.0f;
};

struct Player {
    std::string id;
    std::string nickname;
    int room_id = -1;
    bool is_ready = false;

    // Transform Data
    vec3 body_position;
    quat body_rotation;
    quat camera_rotation;

    // Animation State
    float anim_x = 0.0f;
    float anim_y = 0.0f;
    bool anim_walk = false;
    bool anim_sprint = false;
    bool anim_roll = false;
    bool anim_isGrounded = true;
    bool anim_crouch = false;

    // State Data
    int current_weapon_id = 1; // Default weapon
};

